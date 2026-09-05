<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ config('app.name', 'Murder Mystery') }} — Join Session</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Crimson+Text:ital@0;1&display=swap" rel="stylesheet">
    @vite('resources/css/menu.css')
</head>
<body>
    <div class="vignette"></div>

    <main class="menu-wrap">
        <a href="{{ url('/') }}" class="back-link">&larr; Back to Menu</a>

        <h1 class="title">Join Session</h1>
        <p class="subtitle">Enter the invitation code to step into the mystery.</p>

        <div class="divider"></div>

        <form class="page-form" method="POST" action="{{ url('/game/join') }}">
            @csrf

            <div class="field">
                <label for="session-code">Session Code</label>
                <input type="text" id="session-code" name="session_code" placeholder="e.g. RAVEN-4471" required>
            </div>

            <div class="field">
                <label for="display-name">Your Name</label>
                <input type="text" id="display-name" name="display_name" placeholder="Detective..." required>
            </div>

            <button type="submit" class="menu-btn primary">Join Session</button>
        </form>

        <footer class="meta">v0.1.0 &mdash; The Last Witness</footer>
    </main>
</body>
</html>
