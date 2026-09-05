<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ config('app.name', 'Murder Mystery') }} — Credits</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Crimson+Text:ital@0;1&display=swap" rel="stylesheet">
    @vite('resources/css/menu.css')
</head>
<body>
    <div class="vignette"></div>

    <main class="menu-wrap">
        <a href="{{ url('/') }}" class="back-link">&larr; Back to Menu</a>

        <h1 class="title">Credits</h1>
        <p class="subtitle">Written, designed, and investigated by AdmiralKeso.</p>

        <div class="divider"></div>

        <div class="credits-block">
            <h3>Design &amp; Development</h3>
            <p>AdmiralKeso</p>

            <h3>Fonts</h3>
            <p>Cinzel &amp; Crimson Text, via Google Fonts</p>

            <h3>Built With</h3>
            <p>Laravel, Vite, Unity</p>
        </div>

        <footer class="meta">v0.1.0 &mdash; The Last Witness</footer>
    </main>
</body>
</html>
