using RoadPatcher._keenthemes.libs;

namespace RoadPatcher._keenthemes;

public interface IKTBootstrapBase
{
    void InitThemeMode();
    
    void InitThemeDirection();

    void InitLayout();
    
    void Init(IKTTheme theme);
}