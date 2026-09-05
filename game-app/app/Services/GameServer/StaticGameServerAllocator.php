<?php

namespace App\Services\GameServer;

use App\Models\GameSession;

/**
 * Placeholder allocator: points every session at a single, pre-running
 * Unity server (configured via UNITY_SERVER_HOST / UNITY_SERVER_PORT).
 *
 * There is no dynamic server orchestration yet (no process/container
 * spawning). Swap this binding in AppServiceProvider for a real allocator
 * once a Unity dedicated server build and hosting story exist.
 */
class StaticGameServerAllocator implements GameServerAllocator
{
    public function allocate(GameSession $gameSession): GameServerAddress
    {
        return new GameServerAddress(
            host: config('unity.server_host'),
            port: config('unity.server_port'),
        );
    }
}
