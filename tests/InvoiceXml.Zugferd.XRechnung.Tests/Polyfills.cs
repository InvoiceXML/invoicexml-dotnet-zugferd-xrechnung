#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    // Lets the test project use records when it runs on .NET Framework.
    internal static class IsExternalInit
    {
    }
}
#endif
