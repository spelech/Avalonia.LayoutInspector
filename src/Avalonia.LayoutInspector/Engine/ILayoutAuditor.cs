using Avalonia;
using Avalonia.LayoutInspector.Models;

namespace Avalonia.LayoutInspector.Engine;

public interface ILayoutAuditor
{
    AuditReport Audit(Visual root, AuditOptions? options = null);
}
