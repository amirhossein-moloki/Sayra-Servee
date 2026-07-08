document.addEventListener('DOMContentLoaded', () => {
    // Update Time
    function updateClock() {
        const now = new Date();
        const timeElement = document.getElementById('current-time');
        const dateElement = document.getElementById('current-date');

        if (timeElement) {
            timeElement.textContent = now.getHours().toString().padStart(2, '0') + ':' +
                                   now.getMinutes().toString().padStart(2, '0');
        }

        if (dateElement) {
            dateElement.textContent = now.getFullYear() + '/' +
                                   (now.getMonth() + 1).toString().padStart(2, '0') + '/' +
                                   now.getDate().toString().padStart(2, '0');
        }
    }

    setInterval(updateClock, 1000);
    updateClock();

    // Timer Simulation
    let seconds = 9912; // 02:45:12
    function updateTimer() {
        seconds++;
        const timerElement = document.getElementById('session-timer');
        if (timerElement) {
            const h = Math.floor(seconds / 3600).toString().padStart(2, '0');
            const m = Math.floor((seconds % 3600) / 60).toString().padStart(2, '0');
            const s = (seconds % 60).toString().padStart(2, '0');
            timerElement.textContent = `${h}:${m}:${s}`;
        }
    }
    setInterval(updateTimer, 1000);

    // Interactive Elements
    const gameCards = document.querySelectorAll('.game-card');
    gameCards.forEach(card => {
        card.addEventListener('click', () => {
            if (!card.classList.contains('unavailable')) {
                gameCards.forEach(c => c.classList.remove('selected'));
                card.classList.add('selected');
            }
        });
    });

    const playButtons = document.querySelectorAll('.play-btn');
    playButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            if (!btn.disabled) {
                const gameTitle = btn.closest('.game-details').querySelector('.game-title').textContent;
                alert(`در حال اجرای بازی: ${gameTitle}`);
            }
        });
    });
});
