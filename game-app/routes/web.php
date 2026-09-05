<?php

use Illuminate\Support\Facades\Route;

Route::get('/', function () {
    return view('menu');
});

Route::get('/game/new', function () {
    return view('createsession');
});

Route::get('/game/join', function () {
    return view('joinsession');
});

Route::get('/game/profile', function () {
    return view('profile');
});

Route::get('/game/settings', function () {
    return view('settings');
});

Route::get('/game/credits', function () {
    return view('credits');
});
