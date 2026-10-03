namespace Forzion.Simulation;

/// <summary>
/// One game rule, run once per tick over the whole state. Systems are hand-written, hold no
/// state of their own and visit entities in ascending ID order. The order they run in is the
/// fixed list in <see cref="Match"/>.
/// </summary>
internal interface ISystem
{
    void Run(TickContext context);
}
