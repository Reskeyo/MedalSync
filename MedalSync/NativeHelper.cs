using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MedalSync;

/// <summary>
/// Provides native Windows API access for creating hard links.
/// Hard links create a second directory entry pointing to the same file data,
/// using zero additional disk space. Both the original and the link are equal —
/// deleting one does NOT affect the other.
/// Requirement: both paths must be on the same NTFS volume.
/// </summary>
public static partial class NativeHelper
{
    [LibraryImport("kernel32.dll", EntryPoint = "CreateHardLinkW",
        StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateHardLinkW(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes);

    /// <summary>
    /// Creates a hard link at <paramref name="linkPath"/> pointing to <paramref name="targetPath"/>.
    /// </summary>
    /// <returns>True if the hard link was created successfully.</returns>
    public static bool TryCreateHardLink(string linkPath, string targetPath, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            if (CreateHardLinkW(linkPath, targetPath, IntPtr.Zero))
                return true;

            int errorCode = Marshal.GetLastPInvokeError();
            var ex = new Win32Exception(errorCode);
            errorMessage = $"Win32 Error {errorCode}: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
