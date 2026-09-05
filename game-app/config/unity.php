<?php

return [

    /*
    |--------------------------------------------------------------------------
    | Unity Game Server
    |--------------------------------------------------------------------------
    |
    | Connection details for the Unity dedicated server that game sessions
    | are handed off to once the host presses "Start Game". Until real
    | server orchestration exists, every session is pointed at this single
    | pre-running server.
    |
    */

    'server_host' => env('UNITY_SERVER_HOST', '127.0.0.1'),
    'server_port' => env('UNITY_SERVER_PORT', 7777),

];
