<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ config('app.name', 'Murder Mystery') }} — Main Menu</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Crimson+Text:ital@0;1&display=swap" rel="stylesheet">
    @vite('resources/css/menu.css')
</head>
<body>
    <div class="vignette"></div>

    <main class="menu-wrap">
        <h1 class="title">The Last Witness</h1>

        <div class="divider"></div>

        <nav class="menu">
            <a href="{{ url('/game/new') }}" class="menu-btn primary">Create session</a>
            <a href="{{ url('/game/join') }}" class="menu-btn">Join Session</a>
            <a href="{{ url('/game/profile') }}" class="menu-btn">Profile</a>
            <button type="button" class="menu-btn" onclick="document.getElementById('settings-panel').showModal()">Settings</button>
            <button type="button" class="menu-btn" onclick="document.getElementById('credits-panel').showModal()">Credits</button>
        </nav>

        <footer class="meta">v0.1.0 &mdash; The Last Witness</footer>
    </main>

    <dialog id="settings-panel" class="panel">
        <h2>Settings</h2>
        <p>Audio, video, and control options will live here.</p>
        <button type="button" class="close-btn" onclick="document.getElementById('settings-panel').close()">Close</button>
    </dialog>

    <dialog id="credits-panel" class="panel">
        <h2>Credits</h2>
        <p>Written, designed, and investigated by the {{ config('app.name', 'Murder Mystery') }} team.</p>
        <button type="button" class="close-btn" onclick="document.getElementById('credits-panel').close()">Close</button>
    </dialog>
</body>
</html>
