using System;

// Plain data-transfer objects matching game-app's routes/api.php JSON shapes.
// Kept as [Serializable] fields (not properties) because JsonUtility only
// reads/writes public fields.

[Serializable]
public class CreateSessionRequest
{
    public string session_name;
    public int max_players;
    public string scenario;
    public string display_name;
}

[Serializable]
public class JoinSessionRequest
{
    public string display_name;
}

[Serializable]
public class CreateSessionResponse
{
    public string code;
    public string player_token;
    public bool is_host;
}

[Serializable]
public class JoinSessionResponse
{
    public string code;
    public string player_token;
    public bool is_host;
}

[Serializable]
public class SessionPlayerDto
{
    public int id;
    public string display_name;
    public bool is_host;
}

[Serializable]
public class SessionStatusResponse
{
    public string name;
    public string status;
    public SessionPlayerDto[] players;
    public int max_players;
    public string unity_host;
    public int unity_port;
    public bool is_host;
}

[Serializable]
public class StartSessionResponse
{
    public string status;
    public string unity_host;
    public int unity_port;
}

[Serializable]
public class MessageResponse
{
    public string message;
}
