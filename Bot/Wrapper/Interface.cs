using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public interface Bot
{
    (IEnumerable<Action>, IEnumerable<DebugCommand>) OnFrame();
}