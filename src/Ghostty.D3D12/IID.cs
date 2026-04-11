namespace Ghostty.D3D12;

/// <summary>
/// Interface IIDs for D3D12 COM interfaces used in shared texture interop.
/// </summary>
public static class IID
{
    public static readonly Guid ID3D12Device = new("189819f1-1db6-4b57-be54-1821339b85f7");
    public static readonly Guid ID3D12CommandQueue = new("0ec870a6-5d7e-4c22-8cfc-5baae07616ed");
    public static readonly Guid ID3D12CommandAllocator = new("6102dee4-af59-4b09-b999-b44d73f09b24");
    public static readonly Guid ID3D12GraphicsCommandList = new("5b160d0f-ac1b-4185-8ba8-b3ae42a5a455");
    public static readonly Guid ID3D12Resource = new("696442be-a72e-4059-bc79-5b5c98040fad");
    public static readonly Guid ID3D12Fence = new("0a753dcf-c4d8-4b91-adf6-be5a60d95a76");
}