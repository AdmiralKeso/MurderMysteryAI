<?php

namespace App\Services\GameServer;

use App\Models\GameSession;

interface GameServerAllocator
{
    /**
     * Allocate (or start) a Unity game server for the given session and
     * return the address players should connect to.
     */
    public function allocate(GameSession $gameSession): GameServerAddress;
}
