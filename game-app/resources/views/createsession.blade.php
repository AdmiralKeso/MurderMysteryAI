<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ config('app.name', 'Murder Mystery') }} — Create Session</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Crimson+Text:ital@0;1&display=swap" rel="stylesheet">
    @vite('resources/css/menu.css')
</head>
<body>
    <div class="vignette"></div>

    <main class="menu-wrap">
        <a href="{{ url('/') }}" class="back-link">&larr; Back to Menu</a>

        <h1 class="title">Create Session</h1>
        <p class="subtitle">Set the scene before your guests arrive.</p>

        <div class="divider"></div>

        <form class="page-form" method="POST" action="{{ url('/game/new') }}">
            @csrf

            <div class="field">
                <label for="session-name">Session Name</label>
                <input type="text" id="session-name" name="session_name" value="{{ old('session_name') }}" placeholder="The Blackwood Estate" required>
                @error('session_name') <p class="field-error">{{ $message }}</p> @enderror
            </div>

            <div class="field">
                <label for="display-name">Your Name</label>
                <input type="text" id="display-name" name="display_name" value="{{ old('display_name') }}" placeholder="Detective..." required>
                @error('display_name') <p class="field-error">{{ $message }}</p> @enderror
            </div>

            <div class="field">
                <label for="max-players">Max Players</label>
                <input type="number" id="max-players" name="max_players" min="3" max="12" value="{{ old('max_players', 6) }}" required>
                @error('max_players') <p class="field-error">{{ $message }}</p> @enderror
            </div>

            <div class="field">
                <label for="scenario">Scenario</label>
                <select id="scenario" name="scenario">
                    <option value="random" @selected(old('scenario') === 'random')>Random</option>
                    <option value="manor" @selected(old('scenario') === 'manor')>The Manor Murder</option>
                    <option value="cruise" @selected(old('scenario') === 'cruise')>Death on the Cruise</option>
                </select>
            </div>

            <button type="submit" class="menu-btn primary">Create Session</button>
        </form>

        <footer class="meta">v0.1.0 &mdash; The Last Witness</footer>
    </main>
</body>
</html>
