<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ config('app.name', 'Murder Mystery') }} — {{ $gameSession->name }}</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Crimson+Text:ital@0;1&display=swap" rel="stylesheet">
    @vite(['resources/css/menu.css', 'resources/js/app.js'])
</head>
<body>
    <div class="vignette"></div>

    <main
        class="menu-wrap"
        id="room"
        data-players-url="{{ route('session.players', $gameSession->code) }}"
        data-start-url="{{ route('session.start', $gameSession->code) }}"
        data-is-host="{{ $isHost ? '1' : '0' }}"
        data-csrf-token="{{ csrf_token() }}"
    >
        <form method="POST" action="{{ route('session.leave', $gameSession->code) }}">
            @csrf
            <button type="submit" class="back-link back-link-btn">&larr; Back to Menu</button>
        </form>

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
</body>
</html>
