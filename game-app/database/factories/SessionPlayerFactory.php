<?php

namespace Database\Factories;

use App\Models\GameSession;
use App\Models\SessionPlayer;
use Illuminate\Database\Eloquent\Factories\Factory;

/**
 * @extends Factory<SessionPlayer>
 */
class SessionPlayerFactory extends Factory
{
    /**
     * Define the model's default state.
     *
     * @return array<string, mixed>
     */
    public function definition(): array
    {
        return [
            'game_session_id' => GameSession::factory(),
            'display_name' => $this->faker->firstName(),
            'is_host' => false,
        ];
    }
}
