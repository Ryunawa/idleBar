using System;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Online;

namespace IdleBar.Ui;

public static class SocialControls
{
    public static void Attach(Node host, GameBridge game, ExpandedBar bar, Func<float> scale)
    {
        if (game.Rooms is not RoomDesk rooms)
        {
            return;
        }

        PlayerMenu players = new();
        host.AddChild(players);
        ChatBox chat = new();
        host.AddChild(chat);
        OrderMenu orders = new();
        host.AddChild(orders);
        orders.Ordered += rooms.Order;
        bar.OrderRequested += point =>
        {
            if (rooms.Room is { Mine: false } room)
            {
                orders.Open(room, point);
            }
        };
        players.ProfileRequested += game.OpenProfile;
        players.MuteRequested += rooms.Mute;
        players.DoorRequested += rooms.ShowDoor;
        players.LeaveRequested += rooms.Leave;
        bar.Lane.PlayerMenuRequested += (patron, point) =>
        {
            if (Choice(game, patron) is PlayerChoice choice)
            {
                players.Open(choice, point);
            }
        };
        chat.Submitted += rooms.Say;
        bar.ChatRequested += anchor => chat.Open(anchor, scale());
        bar.LeaveRequested += rooms.Leave;
        game.Spoken += line => bar.Lane.Speak(line.Author, line.Name, line.Text);
    }

    private static PlayerChoice? Choice(GameBridge game, Patron patron)
    {
        if (game.PlayerOf(patron) is not Guid player)
        {
            return null;
        }

        TavernData? data = game.Data;
        long? door = !game.Away && patron.Visit is long visit ? visit : null;
        return new PlayerChoice(
            player,
            patron.Guest ?? "Ce joueur",
            player == game.Me,
            data?.Friends.Any(friend => friend.Id == player) == true,
            data?.Muted?.Any(muted => muted.Id == player) == true,
            door);
    }
}
