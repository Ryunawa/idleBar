using Godot;
using IdleBar.Inn;

namespace IdleBar.Ui;

public partial class TavernLane
{
    public override void _GuiInput(InputEvent @event)
    {
        if (_tavern is null)
        {
            return;
        }

        switch (@event)
        {
            case InputEventMouseMotion motion:
                _hovered = StationAt(motion.Position);
                _hoveredPatron = _hovered < 0 ? PatronAt(motion.Position) : null;
                _hoveredHelper = _hovered < 0 && _hoveredPatron is null && LaneHits.Helper(_tavern, ArtPixel(motion.Position), _top);
                MouseDefaultCursorShape = _hovered >= 0 ? CursorShape.PointingHand : CursorShape.Arrow;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press when WalkAt(press.Position) is StreetWalk walk:
                AcceptEvent();
                PasserbyClicked?.Invoke(walk.Passerby, ScreenPoint(press.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press when StationAt(press.Position) < 0 && PatronAt(press.Position) is Patron clicked:
                AcceptEvent();
                PatronClicked?.Invoke(clicked);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } press when PatronAt(press.Position) is Patron player && PlayerOf(player) is not null:
                AcceptEvent();
                PlayerMenuRequested?.Invoke(player, ScreenPoint(press.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press when StationAt(press.Position) >= 0:
                AcceptEvent();
                _pressed = StationAt(press.Position);
                _tavern.Press(_pressed);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when _pressed >= 0:
                AcceptEvent();
                _tavern.Release(_pressed);
                _pressed = -1;
                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit)
        {
            _hovered = -1;
            _hoveredPatron = null;
            _hoveredHelper = false;
        }
    }

    private Vector2I ScreenPoint(Vector2 position) =>
        GetWindow().Position + (Vector2I)((GetGlobalPosition() + position) * GetWindow().ContentScaleFactor);

    private Vector2I ArtPixel(Vector2 position) => new((int)(position.X / _artScale), (int)(position.Y / _artScale));

    private StreetWalk? WalkAt(Vector2 position) => LaneHits.Walk(Street, ArtPixel(position), _top);

    private int StationAt(Vector2 position) => _tavern!.Interactive ? LaneHits.Station(_tavern, ArtPixel(position), _top) : -1;

    private Patron? PatronAt(Vector2 position) => LaneHits.Patron(_tavern!, ArtPixel(position), _top);
}
