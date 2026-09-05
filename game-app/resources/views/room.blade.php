<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ config('app.name', 'Murder Mystery') }} — {{ $gameSession->name }}</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Crimson+Text:ital@0;1&display=swap" rel="stylesheet">
    @vite('resources/css/menu.css')
</head>
<body>
    <div class="vignette"></div>

    <main class="menu-wrap">
        <a href="{{ url('/') }}" class="back-link">&larr; Back to Menu</a>

        <h1 class="title">{{ $gameSession->name }}</h1>
        <p class="subtitle">Share the code below to invite your guests.</p>
        <div class="room-code" id="room-code">{{ $gameSession->code }}</div>

        <div class="divider"></div>

        <ul class="player-list" id="player-list"></ul>

        <p class="room-status" id="room-status"></p>

        @if ($isHost)
            <button type="button" class="menu-btn primary" id="start-btn">Start Game</button>
        @endif

        <footer class="meta">v0.1.0 &mdash; The Last Witness</footer>
    </main>

    <script>
        const code = @json($gameSession->code);
        const isHost = @json($isHost);
        const playersUrl = @json(route('session.players', $gameSession->code));
        const startUrl = @json(route('session.start', $gameSession->code));
        const csrfToken = @json(csrf_token());

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
                statusEl.textContent = `Waiting for players… (${data.players.length}/${data.max_players})`;
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
    </script>
</body>
</html>
