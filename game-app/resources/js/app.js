document.addEventListener('DOMContentLoaded', () => {
    const room = document.getElementById('room');

    if (!room) {
        return;
    }

    const { playersUrl, startUrl, csrfToken } = room.dataset;
    const isHost = room.dataset.isHost === '1';

    const listEl = document.getElementById('player-list');
    const statusEl = document.getElementById('room-status');
    const startBtn = document.getElementById('start-btn');

    let polling = true;

    function renderPlayers(data) {
        listEl.innerHTML = '';
        data.players.forEach((player) => {
            const li = document.createElement('li');
            li.className = 'player-row';
            li.innerHTML = `<span>${player.display_name}</span>` +
                (player.is_host ? '<span class="host-tag">Host</span>' : '');
            listEl.appendChild(li);
        });

        if (data.status === 'lobby') {
            statusEl.classList.remove('error');
            statusEl.textContent = isHost
                ? `Waiting for players… (${data.players.length}/${data.max_players})`
                : `Waiting for host to start the game… (${data.players.length}/${data.max_players})`;
            if (startBtn) {
                startBtn.disabled = data.players.length < 3;
            }
        } else if (data.status === 'in_progress') {
            polling = false;
            statusEl.classList.remove('error');
            statusEl.innerHTML = 'Unity is taking over this session.' +
                (data.unity_host ? `<div class="connect-info">${data.unity_host}:${data.unity_port}</div>` : '');
            if (startBtn) {
                startBtn.remove();
            }
        }
    }

    async function refresh() {
        try {
            const response = await fetch(playersUrl);
            const data = await response.json();
            renderPlayers(data);
        } catch (e) {
            // transient network hiccup — try again on the next tick
        }

        if (polling) {
            setTimeout(refresh, 2500);
        }
    }

    if (startBtn) {
        startBtn.addEventListener('click', async () => {
            startBtn.disabled = true;
            try {
                const response = await fetch(startUrl, {
                    method: 'POST',
                    headers: {
                        'X-CSRF-TOKEN': csrfToken,
                        Accept: 'application/json',
                    },
                });
                const data = await response.json();

                if (!response.ok) {
                    statusEl.classList.add('error');
                    statusEl.textContent = data.message ?? 'Could not start the game.';
                    startBtn.disabled = false;
                    return;
                }

                refresh();
            } catch (e) {
                statusEl.classList.add('error');
                statusEl.textContent = 'Could not reach the server.';
                startBtn.disabled = false;
            }
        });
    }

    refresh();
});
