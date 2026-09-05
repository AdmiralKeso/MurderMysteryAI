<?php

namespace App\Models;

use Database\Factories\GameSessionFactory;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;

class GameSession extends Model
{
    /** @use HasFactory<GameSessionFactory> */
    use HasFactory;

    protected $fillable = [
        'code',
        'name',
        'scenario',
        'max_players',
        'status',
        'unity_host',
        'unity_port',
        'started_at',
    ];

    protected function casts(): array
    {
        return [
            'max_players' => 'integer',
            'unity_port' => 'integer',
            'started_at' => 'datetime',
        ];
    }

    /**
     * @return HasMany<SessionPlayer, $this>
     */
    public function players(): HasMany
    {
        return $this->hasMany(SessionPlayer::class);
    }

    public function isFull(): bool
    {
        return $this->players()->count() >= $this->max_players;
    }

    public function isLobby(): bool
    {
        return $this->status === 'lobby';
    }

    public static function generateUniqueCode(): string
    {
        $words = ['RAVEN', 'MANOR', 'SHADOW', 'CIPHER', 'ASHFORD', 'GASLIGHT', 'CRYPT', 'VELVET', 'ORCHID', 'MIDNIGHT'];

        do {
            $code = $words[array_rand($words)].'-'.random_int(1000, 9999);
        } while (self::where('code', $code)->exists());

        return $code;
    }
}
