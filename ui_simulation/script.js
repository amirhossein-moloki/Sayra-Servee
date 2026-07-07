document.addEventListener('DOMContentLoaded', () => {
    // Search functionality simulation
    const searchBox = document.querySelector('.search-box');
    const gameCards = document.querySelectorAll('.game-card');

    searchBox.addEventListener('input', (e) => {
        const query = e.target.value.toLowerCase();
        gameCards.forEach(card => {
            const name = card.querySelector('.game-name').textContent.toLowerCase();
            if (name.includes(query)) {
                card.style.display = 'block';
            } else {
                card.style.display = 'none';
            }
        });
    });

    // Button hover and click effects
    const buttons = document.querySelectorAll('button');
    buttons.forEach(btn => {
        btn.addEventListener('click', () => {
            if (!btn.classList.contains('unavailable')) {
                btn.style.transform = 'scale(0.95)';
                setTimeout(() => {
                    btn.style.transform = 'scale(1)';
                    alert(`Action triggered: ${btn.textContent}`);
                }, 100);
            }
        });
    });

    // Dynamic Time Update
    const timeElement = document.querySelector('.time');
    setInterval(() => {
        const now = new Date();
        timeElement.textContent = now.getHours().toString().padStart(2, '0') + ':' +
                                now.getMinutes().toString().padStart(2, '0');
    }, 1000);
});
