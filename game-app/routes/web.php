<?php

use App\Http\Controllers\GameSessionController;
use Illuminate\Support\Facades\Route;

Route::get('/', function () {
    return view('menu');
});

Route::get('/game/new', function () {
    return view('createsession');
});
Route::post('/game/new', [GameSessionController::class, 'store'])->name('session.store');

Route::get('/game/join', function () {
    return view('joinsession');
});
Route::post('/game/join', [GameSessionController::class, 'join'])->name('session.join');

Route::get('/game/session/{code}', [GameSessionController::class, 'show'])->name('session.show');
Route::get('/game/session/{code}/players', [GameSessionController::class, 'players'])->name('session.players');
Route::post('/game/session/{code}/start', [GameSessionController::class, 'start'])->name('session.start');
Route::post('/game/session/{code}/leave', [GameSessionController::class, 'leave'])->name('session.leave');

Route::get('/game/profile', function () {
    return view('profile');
});

Route::get('/game/settings', function () {
    return view('settings');
});

Route::get('/game/credits', function () {
    return view('credits');
});
