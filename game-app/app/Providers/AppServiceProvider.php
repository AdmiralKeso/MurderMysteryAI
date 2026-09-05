<?php

namespace App\Providers;

use App\Services\GameServer\GameServerAllocator;
use App\Services\GameServer\StaticGameServerAllocator;
use Illuminate\Support\ServiceProvider;

class AppServiceProvider extends ServiceProvider
{
    /**
     * Register any application services.
     */
    public function register(): void
    {
        $this->app->bind(GameServerAllocator::class, StaticGameServerAllocator::class);
    }

    /**
     * Bootstrap any application services.
     */
    public function boot(): void
    {
        //
    }
}
