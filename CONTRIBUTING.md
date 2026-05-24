# Contributing

# Code Conventions
- Follow consistent formatting and indentation to enhance readability.
- Include comments and documentation to explain complex logic and the purpose of functions and classes.
	- Though it'd be better to make the code more self-documenting, reducing the need for comments
	- Ensure that the comments focus on explaining the "why" behind the code rather than the "what," 
	as the code itself should ideally convey the latter.
- Use docstrings to provide detailed explanations of functions, classes, and modules, including
 their purpose, and and any exceptions they may raise.
	- Best to name the parameters so there's no need to explain them further in the docstrings.
	- This is especially important for public APIs, where users may not have access to the underlying helpers
- Naming
	- Descriptive and clearly indicate the purpose of the variable, function, or class.
	- Classes and functions: UpperPascalCase (e.g., `MyClass`)
	- For names that have acronyms, capitalize only the first letter (e.g., `HttpFmodClient` over 'HTTPFMODClient').
		- This is especially helpful for names that have one acronym following another
	- Non-public variables: camelCase preceded by an underscore (e.g., `_myVariable`)
	- Public variables: camelCase without an underscore (e.g., `myVariable`)
	- Properties (Public or otherwise): UpperCamelCase (e.g., `MyProperty`)
	- Function args and local vars: camelCase (e.g., `myArg`)
- Use local functions to break down complex functions into smaller, more manageable pieces.
	- For helper functionality that'd be used by multiple other funcs, consider making it a 
	private class function instead
- If the code is clear and self-explanatory, avoid adding comments that simply restate what the code does.
	- In other words: don't be a Captain Obvious
- The following types of fields and functions should be protected instead of private:
	- Lifecycle hooks (e.g., `Awake`, `Start`, `Update`, etc.)
	- Serialized fields
		- Good chance that derived classes will need access to at least some of those
	- Callbacks (e.g., `OnTriggerEnter`, `OnCollisionEnter`, etc.)

## Project Organization Guidelines
- When proposing folder reorganizations for scripts, avoid creating a new folder for a single script.
- Only suggest moving a script into a newly created folder when at least **four other scripts** should also 
be moved into that same new folder (minimum five scripts total).
- Prefer existing folders when only one or two scripts are involved.