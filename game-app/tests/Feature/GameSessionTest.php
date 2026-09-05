<?php

namespace Tests\Feature;

use App\Models\GameSession;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class GameSessionTest extends TestCase
{
    use RefreshDatabase;

    public function test_creating_a_session_creates_a_room_with_the_host_as_a_player(): void
    {
        $response = $this->post('/game/new', [
            'session_name' => 'The Blackwood Estate',
            'display_name' => 'Detective Vance',
            'max_players' => 6,
            'scenario' => 'manor',
        ]);

        $gameSession = GameSession::firstOrFail();

        $response->assertRedirect(route('session.show', $gameSession->code));
        $this->assertSame('The Blackwood Estate', $gameSession->name);
        $this->assertCount(1, $gameSession->players);
        $this->assertTrue($gameSession->players->first()->is_host);
    }

    public function test_another_player_can_join_an_open_session_by_code(): void
    {
        $gameSession = GameSession::factory()->create();
        $gameSession->players()->create(['display_name' => 'Host', 'is_host' => true]);

        $response = $this->post('/game/join', [
            'session_code' => $gameSession->code,
            'display_name' => 'Detective Vance',
        ]);

        $response->assertRedirect(route('session.show', $gameSession->code));
        $this->assertSame(2, $gameSession->players()->count());
    }

    public function test_joining_with_an_unknown_code_fails(): void
    {
        $response = $this->post('/game/join', [
            'session_code' => 'NOPE-0000',
            'display_name' => 'Detective Vance',
        ]);

        $response->assertSessionHasErrors('session_code');
    }

    public function test_players_endpoint_lists_current_players(): void
    {
        $gameSession = GameSession::factory()->create();
        $gameSession->players()->create(['display_name' => 'Host', 'is_host' => true]);

        $response = $this->getJson(route('session.players', $gameSession->code));

        $response->assertOk()->assertJsonFragment(['display_name' => 'Host', 'is_host' => true]);
    }

    public function test_only_the_host_can_start_the_game(): void
    {
        $gameSession = GameSession::factory()->create();
        $gameSession->players()->createMany([
            ['display_name' => 'Host', 'is_host' => true],
            ['display_name' => 'Guest', 'is_host' => false],
            ['display_name' => 'Guest 2', 'is_host' => false],
        ]);

        $response = $this->postJson(route('session.start', $gameSession->code));

        $response->assertForbidden();
        $this->assertSame('lobby', $gameSession->fresh()->status);
    }

    public function test_host_starting_the_game_allocates_a_unity_server_and_hands_off_the_session(): void
    {
        $this->post('/game/new', [
            'session_name' => 'The Blackwood Estate',
            'display_name' => 'Host',
            'max_players' => 6,
            'scenario' => 'manor',
        ]);

        $gameSession = GameSession::firstOrFail();
        $gameSession->players()->createMany([
            ['display_name' => 'Guest 1', 'is_host' => false],
            ['display_name' => 'Guest 2', 'is_host' => false],
        ]);

        $response = $this->postJson(route('session.start', $gameSession->code));

        $response->assertOk()->assertJsonFragment(['status' => 'in_progress']);

        $gameSession->refresh();
        $this->assertSame('in_progress', $gameSession->status);
        $this->assertNotNull($gameSession->unity_host);
        $this->assertNotNull($gameSession->unity_port);
        $this->assertNotNull($gameSession->started_at);
    }

    public function test_starting_requires_at_least_three_players(): void
    {
        $this->post('/game/new', [
            'session_name' => 'The Blackwood Estate',
            'display_name' => 'Host',
            'max_players' => 6,
            'scenario' => 'manor',
        ]);

        $gameSession = GameSession::firstOrFail();

        $response = $this->postJson(route('session.start', $gameSession->code));

        $response->assertUnprocessable();
        $this->assertSame('lobby', $gameSession->fresh()->status);
    }

    public function test_a_guest_leaving_removes_them_but_keeps_the_session(): void
    {
        $gameSession = GameSession::factory()->create();
        $host = $gameSession->players()->create(['display_name' => 'Host', 'is_host' => true]);
        $guest = $gameSession->players()->create(['display_name' => 'Guest', 'is_host' => false]);

        $response = $this->withSession(["game_sessions.{$gameSession->id}.player_id" => $guest->id])
            ->post(route('session.leave', $gameSession->code));

        $response->assertRedirect('/');
        $this->assertModelMissing($guest);
        $this->assertTrue($host->fresh()->is_host);
    }

    public function test_the_host_leaving_promotes_another_player(): void
    {
        $gameSession = GameSession::factory()->create();
        $host = $gameSession->players()->create(['display_name' => 'Host', 'is_host' => true]);
        $guest = $gameSession->players()->create(['display_name' => 'Guest', 'is_host' => false]);

        $response = $this->withSession(["game_sessions.{$gameSession->id}.player_id" => $host->id])
            ->post(route('session.leave', $gameSession->code));

        $response->assertRedirect('/');
        $this->assertModelMissing($host);
        $this->assertTrue($guest->fresh()->is_host);
    }

    public function test_the_last_player_leaving_deletes_the_session(): void
    {
        $gameSession = GameSession::factory()->create();
        $host = $gameSession->players()->create(['display_name' => 'Host', 'is_host' => true]);

        $response = $this->withSession(["game_sessions.{$gameSession->id}.player_id" => $host->id])
            ->post(route('session.leave', $gameSession->code));

        $response->assertRedirect('/');
        $this->assertModelMissing($gameSession);
    }

    public function test_leaving_without_a_tracked_player_just_redirects_home(): void
    {
        $gameSession = GameSession::factory()->create();
        $gameSession->players()->create(['display_name' => 'Host', 'is_host' => true]);

        $response = $this->post(route('session.leave', $gameSession->code));

        $response->assertRedirect('/');
        $this->assertSame(1, $gameSession->players()->count());
    }
}
