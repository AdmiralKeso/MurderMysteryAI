<?php

namespace App\Services\GameServer;

final readonly class GameServerAddress
{
    public function __construct(
        public string $host,
        public int $port,
    ) {}
}
