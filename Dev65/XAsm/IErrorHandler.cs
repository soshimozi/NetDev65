namespace Dev65.XAsm;

/**
 * The <CODE>ErrorHandler</CODE> interface defines standard methods for
 * reporting errors and warnings.
 * 
 * author	Andrew Jacobs
 * version	$Id$
 */
public interface IErrorHandler
{
    /**
	 * Generate an error message using the indicated string.
	 * 
	 * @param message	The error message string.
	 */
    void Error(string message);

    /**
	 * Generate a warning message using the indicated string.
	 * 
	 * @param message	The warning message string.
	 */
    void Warning(string message);
}

public class AssemblerEventArgs : EventArgs
{
	public string Message { get; set; }

	public AssemblerEventArgs(string message)
    {
        Message = message;
    }

    public AssemblerEventArgs()
    {
        Message = string.Empty;
    }
}