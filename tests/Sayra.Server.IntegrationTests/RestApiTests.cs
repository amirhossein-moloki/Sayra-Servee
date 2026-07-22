using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Sayra.Server.AdminAPI.Controllers;
using Sayra.Server.AdminAPI.Authentication;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.Session;
using Sayra.Server.Billing.Services;
using Sayra.Server.Licensing.Services;
using Sayra.Server.Configuration.Models;
using Sayra.Server.UpdateSystem.Services;

namespace Sayra.Server.IntegrationTests;

public class RestApiTests
{
    private readonly Mock<IClientRepository> _clientRepoMock = new();
    private readonly Mock<ISessionRepository> _sessionRepoMock = new();
    private readonly Mock<ICommandRepository> _commandRepoMock = new();
    private readonly Mock<ITelemetryRepository> _telemetryRepoMock = new();
    private readonly Mock<IAdminUserRepository> _adminUserRepoMock = new();
    private readonly Mock<IClientRegistry> _clientRegistryMock = new();
    private readonly Mock<ISessionRegistry> _sessionRegistryMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();

    private readonly SessionManager _sessionManager;

    public RestApiTests()
    {
        _sessionManager = new SessionManager(_sessionRegistryMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public void Secured_Controllers_Should_Have_Authorize_Attribute()
    {
        var securedControllers = new[]
        {
            typeof(BillingController),
            typeof(ClientsController),
            typeof(CommandsController),
            typeof(ConfigController),
            typeof(LicenseController),
            typeof(MonitoringController),
            typeof(SessionsController)
        };

        foreach (var controllerType in securedControllers)
        {
            var hasAuthorize = Attribute.IsDefined(controllerType, typeof(AuthorizeAttribute));
            Assert.True(hasAuthorize, $"{controllerType.Name} should have [Authorize] attribute.");
        }
    }

    [Fact]
    public void Public_Controllers_Should_Not_Have_Authorize_Attribute()
    {
        var publicControllers = new[]
        {
            typeof(AuthController),
            typeof(ReservationsController),
            typeof(UpdatesController),
            typeof(AdvertisementsController)
        };

        foreach (var controllerType in publicControllers)
        {
            var hasAuthorize = Attribute.IsDefined(controllerType, typeof(AuthorizeAttribute));
            Assert.False(hasAuthorize, $"{controllerType.Name} should not have [Authorize] attribute.");
        }
    }

    // --- AUTH CONTROLLER ---
    [Fact]
    public async Task Login_With_Valid_Credentials_Returns_200_OK()
    {
        _adminUserRepoMock.Setup(r => r.GetByUsernameAsync("admin"))
            .ReturnsAsync(new AdminUserEntity { Username = "admin", PasswordHash = "secret" });

        var controller = new AuthController(_adminUserRepoMock.Object);
        var request = new LoginRequest("admin", "secret");

        var result = await controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthTokenResponse>(okResult.Value);
        Assert.Equal("dummy-jwt-token", response.AccessToken);
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Returns_401_Unauthorized()
    {
        _adminUserRepoMock.Setup(r => r.GetByUsernameAsync("admin"))
            .ReturnsAsync(new AdminUserEntity { Username = "admin", PasswordHash = "secret" });

        var controller = new AuthController(_adminUserRepoMock.Object);
        var request = new LoginRequest("admin", "wrong_password");

        var result = await controller.Login(request);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        Assert.Equal("UNAUTHORIZED", response.Code);
    }

    [Fact]
    public async Task Login_With_Locked_User_Returns_423_Locked()
    {
        var controller = new AuthController(_adminUserRepoMock.Object);
        var request = new LoginRequest("locked_user", "password");

        var result = await controller.Login(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(423, objectResult.StatusCode);
        var response = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal("ACCOUNT_LOCKED", response.Code);
    }

    // --- RESERVATIONS CONTROLLER ---
    [Fact]
    public void ValidateReservation_With_Valid_User_Returns_200_OK()
    {
        var controller = new ReservationsController();
        var result = controller.Validate("amir", "R-101");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ReservationValidationResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("amir", response.Reservation.Username);
    }

    [Fact]
    public void ValidateReservation_With_Invalid_User_Returns_404_NotFound()
    {
        var controller = new ReservationsController();
        var result = controller.Validate("unknown_user", "R-101");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal("RESERVATION_NOT_FOUND", response.Code);
    }

    [Fact]
    public void ValidateReservation_With_Missing_User_Returns_400_BadRequest()
    {
        var controller = new ReservationsController();
        var result = controller.Validate("", "R-101");

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("BAD_REQUEST", response.Code);
    }

    // --- CLIENTS CONTROLLER ---
    [Fact]
    public async Task GetClients_Returns_Paginated_Clients()
    {
        var clients = new List<ClientEntity>
        {
            new() { PcId = "PC-1", MacAddress = "AA:BB", Hostname = "H-1", IP = "1.1", Status = "Online", LastSeen = DateTime.UtcNow },
            new() { PcId = "PC-2", MacAddress = "CC:DD", Hostname = "H-2", IP = "1.2", Status = "Offline", LastSeen = DateTime.UtcNow }
        };
        _clientRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(clients);

        var controller = new ClientsController(_clientRepoMock.Object, _clientRegistryMock.Object);
        var result = await controller.GetAll(page: 1, limit: 1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<ClientResponse>>(okResult.Value);
        Assert.Single(response);
        Assert.Equal("PC-1", response.First().PcId);
    }

    [Fact]
    public async Task GetClients_With_Status_Filter_Works()
    {
        var clients = new List<ClientEntity>
        {
            new() { PcId = "PC-1", MacAddress = "AA:BB", Hostname = "H-1", IP = "1.1", Status = "Online", LastSeen = DateTime.UtcNow },
            new() { PcId = "PC-2", MacAddress = "CC:DD", Hostname = "H-2", IP = "1.2", Status = "Offline", LastSeen = DateTime.UtcNow }
        };
        _clientRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(clients);

        var controller = new ClientsController(_clientRepoMock.Object, _clientRegistryMock.Object);
        var result = await controller.GetAll(page: 1, limit: 10, status: "Offline");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<ClientResponse>>(okResult.Value);
        Assert.Single(response);
        Assert.Equal("PC-2", response.First().PcId);
    }

    [Fact]
    public async Task GetClients_With_Invalid_Params_Returns_400_BadRequest()
    {
        var controller = new ClientsController(_clientRepoMock.Object, _clientRegistryMock.Object);

        var resultPage = await controller.GetAll(page: 0);
        Assert.IsType<BadRequestObjectResult>(resultPage);

        var resultLimit = await controller.GetAll(limit: -5);
        Assert.IsType<BadRequestObjectResult>(resultLimit);

        var resultStatus = await controller.GetAll(status: "SuperOnline");
        Assert.IsType<BadRequestObjectResult>(resultStatus);
    }

    [Fact]
    public async Task RegisterClient_With_Valid_Request_Succeeds()
    {
        var controller = new ClientsController(_clientRepoMock.Object, _clientRegistryMock.Object);
        var request = new RegisterClientRequest("AA:BB:CC", "NEW-PC", "Tehran");

        var result = await controller.Register(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ClientResponse>(createdResult.Value);
        Assert.Equal("AA:BB:CC", response.MacAddress);
        Assert.Equal("NEW-PC", response.Hostname);
    }

    [Fact]
    public async Task GetClientDetails_Returns_404_When_Not_Found()
    {
        _clientRepoMock.Setup(r => r.GetByPcIdAsync("PC-99")).ReturnsAsync((ClientEntity?)null);
        var controller = new ClientsController(_clientRepoMock.Object, _clientRegistryMock.Object);

        var result = await controller.Get("PC-99");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal("NOT_FOUND", response.Code);
    }

    // --- SESSIONS CONTROLLER ---
    [Fact]
    public async Task StartSession_Returns_201_When_Successful()
    {
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SessionEntity>());
        var controller = new SessionsController(_sessionRepoMock.Object, _sessionManager);
        var request = new StartSessionRequest("PC-1", "Plan-1", "User-1");

        var result = await controller.Start(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<SessionResponse>(createdResult.Value);
        Assert.Equal("PC-1", response.PcId);
        Assert.Equal("ACTIVE", response.Status);
    }

    [Fact]
    public async Task StartSession_Returns_409_Conflict_When_PC_InUse()
    {
        var existing = new List<SessionEntity>
        {
            new() { SessionId = "S-1", PcId = "PC-1", Status = "ACTIVE" }
        };
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);
        var controller = new SessionsController(_sessionRepoMock.Object, _sessionManager);
        var request = new StartSessionRequest("PC-1", "Plan-1", "User-1");

        var result = await controller.Start(request);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal("CONFLICT", response.Code);
    }

    // --- COMMANDS CONTROLLER ---
    [Fact]
    public async Task SendCommand_Returns_202_Accepted()
    {
        _clientRepoMock.Setup(r => r.GetByPcIdAsync("PC-1"))
            .ReturnsAsync(new ClientEntity { PcId = "PC-1", Status = "Online" });

        var controller = new CommandsController(_commandRepoMock.Object, _clientRepoMock.Object);
        var request = new SendCommandRequest("PC-1", "LOCK_PC", null);

        var result = await controller.Send(request);

        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        var response = Assert.IsType<CommandResponse>(acceptedResult.Value);
        Assert.Equal("PC-1", response.PcId);
        Assert.Equal("LOCK_PC", response.Action);
    }

    [Fact]
    public async Task SendCommand_To_Unregistered_PC_Returns_404_NotFound()
    {
        _clientRepoMock.Setup(r => r.GetByPcIdAsync("PC-99")).ReturnsAsync((ClientEntity?)null);

        var controller = new CommandsController(_commandRepoMock.Object, _clientRepoMock.Object);
        var request = new SendCommandRequest("PC-99", "LOCK_PC", null);

        var result = await controller.Send(request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal("NOT_FOUND", response.Code);
    }

    // --- BILLING CONTROLLER ---
    [Fact]
    public async Task GetBillingSummary_Returns_Correct_Summary()
    {
        _clientRepoMock.Setup(r => r.GetByPcIdAsync("PC-1"))
            .ReturnsAsync(new ClientEntity { PcId = "PC-1", Status = "InUse" });
        _sessionRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SessionEntity> { new() { SessionId = "S-1", PcId = "PC-1", Status = "ACTIVE", CurrentCost = 5000m } });

        var controller = new BillingController(null!, null!, _clientRepoMock.Object, _sessionRepoMock.Object);
        var result = await controller.GetSummary("PC-1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BillingSummaryResponse>(okResult.Value);
        Assert.Equal("PC-1", response.PcId);
        Assert.Equal(5000m, response.TotalUnpaidAmount);
    }

    // --- CONFIG CONTROLLER ---
    [Fact]
    public void UpdateConfig_With_Invalid_Values_Returns_400_BadRequest()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new SayraConfig());
        var controller = new ConfigController(options);

        var invalidConfig = new SayraConfigResponse(
            Heartbeat: new Sayra.Server.Application.DTOs.HeartbeatConfig(-1, 30),
            Session: new Sayra.Server.Application.DTOs.SessionConfig(1, 60),
            Security: new Sayra.Server.Application.DTOs.SecurityConfig(5, 15, true),
            Scaling: new Sayra.Server.Application.DTOs.ScalingConfig(false, ""),
            Backup: new Sayra.Server.Application.DTOs.BackupConfig(24, "/backups", 30)
        );

        var result = controller.Update(invalidConfig);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("BAD_REQUEST", response.Code);
    }

    // --- ADVERTISEMENTS CONTROLLER ---
    [Fact]
    public void GetAdvertisements_Returns_All_Ads()
    {
        var controller = new AdvertisementsController();
        var result = controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<AdvertisementItem>>(okResult.Value);
        Assert.NotEmpty(response);
        Assert.Equal("AD-101", response.First().Id);
    }
}
