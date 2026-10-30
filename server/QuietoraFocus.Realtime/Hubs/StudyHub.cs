using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Realtime.Hubs;

public sealed class StudyHub : Hub
{
    private static readonly ConcurrentDictionary<string, StudyRoom> Rooms = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> ConnectionToRoom = new();

    public async Task<StudyRoom?> CreateRoom(string roomName, string subject, string objective, int plannedMinutes, string ownerNickname)
    {
        var randomCode = new string(Enumerable.Range(0, 6).Select(_ => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[Random.Shared.Next(32)]).ToArray());
        var room = new StudyRoom
        {
            RoomCode = randomCode,
            RoomName = string.IsNullOrWhiteSpace(roomName) ? "Focused Study Room" : roomName.Trim(),
            Subject = string.IsNullOrWhiteSpace(subject) ? "General Studies" : subject.Trim(),
            Objective = string.IsNullOrWhiteSpace(objective) ? "Complete Study Session" : objective.Trim(),
            PlannedMinutes = plannedMinutes > 0 ? plannedMinutes : 45,
            OwnerNickname = string.IsNullOrWhiteSpace(ownerNickname) ? "Host" : ownerNickname.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var participant = new RoomParticipant
        {
            ConnectionId = Context.ConnectionId,
            Nickname = room.OwnerNickname,
            AvatarEmoji = "👑",
            CurrentTask = room.Objective,
            IsReady = true
        };
        room.Participants.Add(participant);

        Rooms[randomCode] = room;
        ConnectionToRoom[Context.ConnectionId] = randomCode;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{randomCode}");
        return room;
    }

    public async Task<StudyRoom?> JoinRoom(string roomCode, string nickname, string currentTask)
    {
        var code = roomCode?.Trim().ToUpperInvariant() ?? "";
        if (!Rooms.TryGetValue(code, out var room))
        {
            return null;
        }

        var participant = new RoomParticipant
        {
            ConnectionId = Context.ConnectionId,
            Nickname = string.IsNullOrWhiteSpace(nickname) ? "Peer" : nickname.Trim(),
            AvatarEmoji = GetRandomAvatar(),
            CurrentTask = string.IsNullOrWhiteSpace(currentTask) ? "Deep Focus" : currentTask.Trim(),
            IsReady = true
        };

        room.Participants.RemoveAll(p => p.ConnectionId == Context.ConnectionId);
        room.Participants.Add(participant);
        ConnectionToRoom[Context.ConnectionId] = code;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{code}");
        await Clients.Group($"room_{code}").SendAsync("ParticipantJoined", participant);
        await Clients.Group($"room_{code}").SendAsync("RoomStateUpdated", room);

        return room;
    }

    public async Task LeaveRoom(string roomCode)
    {
        var code = roomCode?.Trim().ToUpperInvariant() ?? "";
        if (Rooms.TryGetValue(code, out var room))
        {
            room.Participants.RemoveAll(p => p.ConnectionId == Context.ConnectionId);
            ConnectionToRoom.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{code}");
            await Clients.Group($"room_{code}").SendAsync("ParticipantLeft", Context.ConnectionId);
            await Clients.Group($"room_{code}").SendAsync("RoomStateUpdated", room);

            if (room.Participants.Count == 0)
            {
                Rooms.TryRemove(code, out _);
            }
        }
    }

    public async Task StartSharedSession(string roomCode)
    {
        var code = roomCode?.Trim().ToUpperInvariant() ?? "";
        if (Rooms.TryGetValue(code, out var room))
        {
            room.IsSessionActive = true;
            room.ElapsedSeconds = 0;
            await Clients.Group($"room_{code}").SendAsync("SessionStarted", room.PlannedMinutes);
            await Clients.Group($"room_{code}").SendAsync("RoomStateUpdated", room);
        }
    }

    public async Task SyncTimerTick(string roomCode, int elapsedSeconds)
    {
        var code = roomCode?.Trim().ToUpperInvariant() ?? "";
        if (Rooms.TryGetValue(code, out var room))
        {
            room.ElapsedSeconds = elapsedSeconds;
            await Clients.Group($"room_{code}").SendAsync("TimerUpdated", elapsedSeconds);
        }
    }

    public async Task SendChatMessage(string roomCode, string text)
    {
        var code = roomCode?.Trim().ToUpperInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(text) || !Rooms.TryGetValue(code, out var room)) return;

        var sender = room.Participants.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)?.Nickname ?? "Peer";
        var msg = new RoomChatMessage
        {
            SenderNickname = sender,
            Text = text.Trim(),
            TimestampUtc = DateTime.UtcNow,
            IsSystem = false
        };

        room.ChatMessages.Add(msg);
        if (room.ChatMessages.Count > 100) room.ChatMessages.RemoveAt(0);

        await Clients.Group($"room_{code}").SendAsync("ReceiveChatMessage", msg);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionToRoom.TryRemove(Context.ConnectionId, out var code) && Rooms.TryGetValue(code, out var room))
        {
            room.Participants.RemoveAll(p => p.ConnectionId == Context.ConnectionId);
            await Clients.Group($"room_{code}").SendAsync("ParticipantLeft", Context.ConnectionId);
            await Clients.Group($"room_{code}").SendAsync("RoomStateUpdated", room);

            if (room.Participants.Count == 0)
            {
                Rooms.TryRemove(code, out _);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetRandomAvatar()
    {
        var emojis = new[] { "👩‍💻", "👨‍💻", "👩‍🔬", "👨‍🔬", "📐", "🎨", "📚", "🚀", "🧠", "✨" };
        return emojis[Random.Shared.Next(emojis.Length)];
    }
}
