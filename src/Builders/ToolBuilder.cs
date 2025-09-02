using Lmss.Models.Core;
namespace Lmss.Builders;

/// <summary>
///     Fluent builder for creating tool definitions.
/// </summary>
public class ToolBuilder {
    /// <summary>
    ///     The description of the tool.
    /// </summary>
    string m_description = string.Empty;

    /// <summary>
    ///     The name of the tool.
    /// </summary>
    string m_name = string.Empty;

    /// <summary>
    ///     The parameters associated with the tool.
    /// </summary>
    object m_parameters = new();

    /// <summary>
    ///     Sets the name of the tool.
    /// </summary>
    /// <param name="name">The name to set.</param>
    /// <returns>The current instance of <see cref="ToolBuilder" />.</returns>
    public ToolBuilder WithName(string name) {
        m_name = name;
        return this;
    }

    /// <summary>
    ///     Sets the description of the tool.
    /// </summary>
    /// <param name="description">The description to set.</param>
    /// <returns>The current instance of <see cref="ToolBuilder" />.</returns>
    public ToolBuilder WithDescription(string description) {
        m_description = description;
        return this;
    }

    /// <summary>
    ///     Sets the parameters for the tool.
    /// </summary>
    /// <param name="parameters">The parameters to set.</param>
    /// <returns>The current instance of <see cref="ToolBuilder" />.</returns>
    public ToolBuilder WithParameters(object parameters) {
        m_parameters = parameters;
        return this;
    }

    /// <summary>
    ///     Builds and returns a new <see cref="Tool" /> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the tool name is not specified.
    /// </exception>
    /// <returns>A new instance of <see cref="Tool" />.</returns>
    public Tool Build() {
        if ( string.IsNullOrEmpty( m_name ) ) {
            throw new InvalidOperationException( "Tool name must be specified" );
        }

        return new Tool {
            Function = new FunctionDefinition {
                Name = m_name,
                Description = m_description,
                Parameters = m_parameters,
            },
        };
    }

    /// <summary>
    ///     Creates a new instance of <see cref="ToolBuilder" />.
    /// </summary>
    /// <returns>A new instance of <see cref="ToolBuilder" />.</returns>
    public static ToolBuilder Create() => new();
}