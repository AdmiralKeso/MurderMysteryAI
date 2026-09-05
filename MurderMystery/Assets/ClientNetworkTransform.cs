using Unity.Netcode.Components;

// Standard Netcode for GameObjects pattern for owner-authoritative movement:
// the base NetworkTransform is server-authoritative unless this is overridden.
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
