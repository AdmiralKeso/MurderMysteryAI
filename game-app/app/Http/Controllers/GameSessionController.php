<?php

namespace App\Http\Controllers;

use App\Models\GameSession;
use App\Models\SessionPlayer;
use App\Services\GameServer\GameServerAllocator;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\RedirectResponse;
use Illuminate\Http\Request;
use Illuminate\View\View;

class GameSessionController extends Controller
{
    public function store(Request $request): RedirectResponse
    {
        $data = $request->validate([
            'session_name' => ['required', 'string', 'max:255'],
            'max_players' => ['required', 'integer', 'min:3', 'max:12'],
            'scenario' => ['required', 'string', 'in:random,manor,cruise'],
            'display_name' => ['required', 'string', 'max:255'],
        ]);

        $gameSession = GameSession::create([
            'code' => GameSession::generateUniqueCode(),
            'name' => $data['session_name'],
            'max_players' => $data['max_players'],
            'scenario' => $data['scenario'],
        ]);

        $host = $gameSession->players()->create([
            'display_name' => $data['display_name'],
            'is_host' => true,
        ]);

        $this->rememberPlayer($gameSession, $host);

        return redirect()->route('session.show', $gameSession->code);
    }

    public function join(Request $request): RedirectResponse
    {
        $data = $request->validate([
            'session_code' => ['required', 'string'],
            'display_name' => ['required', 'string', 'max:255'],
        ]);

        $gameSession = GameSession::where('code', strtoupper(trim($data['session_code'])))->first();

        if (! $gameSession) {
            return back()->withInput()->withErrors(['session_code' => 'No session was found with that code.']);
        }

        if (! $gameSession->isLobby()) {
            return back()->withInput()->withErrors(['session_code' => 'That session has already started.']);
        }

        if ($gameSession->isFull()) {
            return back()->withInput()->withErrors(['session_code' => 'That session is full.']);
        }

        $player = $gameSession->players()->create([
            'display_name' => $data['display_name'],
            'is_host' => false,
        ]);

        $this->rememberPlayer($gameSession, $player);

        return redirect()->route('session.show', $gameSession->code);
    }

    public function show(string $code): View
    {
        $gameSession = GameSession::where('code', $code)->with('players')->firstOrFail();

        return view('room', [
            'gameSession' => $gameSession,
            'isHost' => $this->isHost($gameSession),
        ]);
    }

    public function players(string $code): JsonResponse
    {
        $gameSession = GameSession::where('code', $code)->with('players')->firstOrFail();

        return response()->json([
            'status' => $gameSession->status,
            'players' => $gameSession->players->map(fn (SessionPlayer $player) => [
                'id' => $player->id,
                'display_name' => $player->display_name,
                'is_host' => $player->is_host,
            ]),
            'max_players' => $gameSession->max_players,
            'unity_host' => $gameSession->unity_host,
            'unity_port' => $gameSession->unity_port,
        ]);
    }

    public function start(string $code, GameServerAllocator $allocator): JsonResponse
    {
        $gameSession = GameSession::where('code', $code)->firstOrFail();

        if (! $this->isHost($gameSession)) {
            return response()->json(['message' => 'Only the host can start the game.'], 403);
        }

        if (! $gameSession->isLobby()) {
            return response()->json(['message' => 'This session has already started.'], 409);
        }

        if ($gameSession->players()->count() < 3) {
            return response()->json(['message' => 'At least 3 players are needed to start.'], 422);
        }

        $address = $allocator->allocate($gameSession);

        $gameSession->update([
            'status' => 'in_progress',
            'unity_host' => $address->host,
            'unity_port' => $address->port,
            'started_at' => now(),
        ]);

        return response()->json([
            'status' => $gameSession->status,
            'unity_host' => $gameSession->unity_host,
            'unity_port' => $gameSession->unity_port,
        ]);
    }

    private function rememberPlayer(GameSession $gameSession, SessionPlayer $player): void
    {
        session()->put("game_sessions.{$gameSession->id}.player_id", $player->id);
    }

    private function isHost(GameSession $gameSession): bool
    {
        $playerId = session("game_sessions.{$gameSession->id}.player_id");

        if (! $playerId) {
            return false;
        }

        return $gameSession->players()->whereKey($playerId)->where('is_host', true)->exists();
    }
}
