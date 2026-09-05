<?php

namespace App\Models;

use Database\Factories\SessionPlayerFactory;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class SessionPlayer extends Model
{
    /** @use HasFactory<SessionPlayerFactory> */
    use HasFactory;

    protected $fillable = [
        'game_session_id',
        'display_name',
        'is_host',
    ];

    protected function casts(): array
    {
        return [
            'is_host' => 'boolean',
        ];
    }

    /**
     * @return BelongsTo<GameSession, $this>
     */
    public function gameSession(): BelongsTo
    {
        return $this->belongsTo(GameSession::class);
    }
}
