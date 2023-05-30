namespace Dev65.XAsm;

/**
 * The <CODE>Error</CODE> class contains all the error message strings used by
 * the <CODE>Assembler</CODE>.
 * 
 * @author 	Andrew Jacobs
 * @version	$Id$
 */
public static class ErrorMessage
{
    public const string WRN_LABEL_IGNORED = "This statement cannot be labelled";
    public const string ERR_UNKNOWN_OPCODE = "Unknown opcode or directive";
    public const string ERR_NO_SECTION = "No active section";
    public const string ERR_CONSTANT_EXPR = "Constant expression required";
    public const string ERR_NO_OPEN_IF = ".ELSE or .ENDIF with no matching .IF";
    public const string ERR_CLOSING_PAREN = "Closing parenthesis missing in expression";
    public const string ERR_NO_GLOBAL = "A local label must be preceded by normal label";
    public const string ERR_UNDEF_SYMBOL = "Undefined symbol: ";
    public const string ERR_LABEL_REDEFINED = "Label has already been defined: ";
    public const string ERR_FAILED_TO_FIND_FILE = "Failed to find specified file";
    public const string ERR_EXPECTED_QUOTED_FILENAME = "Expected quoted filename";
    public const string ERR_INSERT_IO_ERROR = "I/O error while inserting binary data";
    public const string ERR_INVALID_EXPRESSION = "Invalid expression";
    public const string WRN_LABEL_IS_A_RESERVED_WORD = "This label is a reserved word";
    public const string ERR_EXPECTED_QUOTED_MESSAGE = "Expected quoted message string";

}
