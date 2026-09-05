<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ config('app.name', 'Murder Mystery') }} — Settings</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Crimson+Text:ital@0;1&display=swap" rel="stylesheet">
    @vite('resources/css/menu.css')
</head>
<body>
    <div class="vignette"></div>

    <main class="menu-wrap">
        <a href="{{ url('/') }}" class="back-link">&larr; Back to Menu</a>

        <h1 class="title">Settings</h1>
        <p class="subtitle">Audio, video, and control options.</p>

        <div class="divider"></div>

        <form class="page-form" method="POST" action="{{ url('/game/settings') }}">
            @csrf

            <div class="field">
                <label for="master-volume">Master Volume</label>
                <input type="range" id="master-volume" name="master_volume" min="0" max="100" value="80">
            </div>

            <div class="field">
                <label for="master-volume">Effects Volume</label>
                <input type="range" id="master-volume" name="master_volume" min="0" max="100" value="80">
            </div>

            <div class="field">
                <label for="music-volume">Music Volume</label>
                <input type="range" id="music-volume" name="music_volume" min="0" max="100" value="60">
            </div>

            <div class="field">
                <select id="screentype" name="screentype">
                    <option value="fullscreen">Fullscreen</option>
                    <option value="windowed">Windowed</option>
                </select>
            </div>

            <div class="field">
                <label for="resolution">Resolution</label>
                <select id="resolution" name="resolution">
                    <option value="1920x1080">1920 x 1080</option>
                    <option value="1600x900">1600 x 900</option>
                    <option value="1280x720">1280 x 720</option>
                </select>
            </div>

            <button type="submit" class="menu-btn primary">Save Settings</button>
        </form>

        <footer class="meta">v0.1.0 &mdash; The Last Witness</footer>
    </main>
</body>
</html>
