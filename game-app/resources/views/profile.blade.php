<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ config('app.name', 'Murder Mystery') }} — Profile</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Crimson+Text:ital@0;1&display=swap" rel="stylesheet">
    @vite('resources/css/menu.css')
</head>
<body>
    <div class="vignette"></div>

    <main class="menu-wrap">
        <a href="{{ url('/') }}" class="back-link">&larr; Back to Menu</a>

        <div class="crest">&#128100;</div>

        <h1 class="title">{{ auth()->user()->name ?? 'Unknown Detective' }}</h1>
        <p class="subtitle">Case history and standing.</p>

        <div class="divider"></div>

        <div class="stat-list">
            <div class="stat-row">
                <span class="stat-label">Sessions Played</span>
                <span class="stat-value">0</span>
            </div>
            <div class="stat-row">
                <span class="stat-label">Cases Solved</span>
                <span class="stat-value">0</span>
            </div>
            <div class="stat-row">
                <span class="stat-label">Times as Murderer</span>
                <span class="stat-value">0</span>
            </div>
        </div>

        <footer class="meta">v0.1.0 &mdash; The Last Witness</footer>
    </main>
</body>
</html>
