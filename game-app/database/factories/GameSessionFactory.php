<?php

namespace Database\Factories;

use App\Models\GameSession;
use Illuminate\Database\Eloquent\Factories\Factory;

/**
 * @extends Factory<GameSession>
 */
class GameSessionFactory extends Factory
{
    /**
     * Define the model's default state.
     *
     * @return array<string, mixed>
     */
    public function definition(): array
    {
        return [
            'code' => GameSession::generateUniqueCode(),
            'name' => $this->faker->words(3, true),
            'scenario' => $this->faker->randomElement(['random', 'manor', 'cruise']),
            'max_players' => $this->faker->numberBetween(3, 12),
            'status' => 'lobby',
        ];
    }
}
