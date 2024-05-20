using DbIlay.Context;
using DbIlay.Models;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace IlayBot {
    public class TelegramBot : ITelegramBot {
        private NotesDbContext _db;
        private readonly ILogger _logger;
        private readonly TelegramBotClient botClient;
        private Dictionary<string, Commands> commandMappings = new Dictionary<string, Commands> {
            { "/start", Commands.start},
            { "Добавить заметку", Commands.createNote },
            { "Получить заметки", Commands.getNotes },
        };
        private Dictionary<long, UserState> _userState = new();
        private Dictionary<long, Note> _lastNote = new();
        private Dictionary<long, int> _userPage = new Dictionary<long, int>(); // Хранение текущей страницы пользователя

        public TelegramBot(NotesDbContext dbContext, ILogger logger, TelegramBotClient botClient) {
            _db = dbContext;
            _logger = logger;
            this.botClient = botClient;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken) {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;
            var chatId = message.Chat.Id;
            GoToState(chatId, messageText);
        }

        private async Task GoToState(long chatId, string messageText) {
            if (!_userState.ContainsKey(chatId)) {
                _userState.Add(chatId, UserState.idle);
            }
            if (messageText == "Меню") {
                await botClient.SendTextMessageAsync(chatId, "Главное меню", replyMarkup: GetKeyboard());
                _userState[chatId] = UserState.idle;
                return;
            }
            _logger.Debug(_userState[chatId]);
            switch (_userState[chatId]) {
                case UserState.createNote:
                    CreateNoteAsync(messageText, chatId);
                    break;
                case UserState.createNoteTitle:
                    SetNoteTitleAsync(messageText, chatId);
                    break;
                case UserState.createNoteText:
                    SetNoteTextAsync(messageText, chatId);
                    break;
                case UserState.editNote:
                    await ExecuteBotCommand(messageText, chatId);
                    break;
                case UserState.editNoteTitle:
                    await EditNoteTitleAsync(chatId, messageText);
                    break;
                case UserState.editNoteText:
                    await EditNoteTextAsync(chatId, messageText);
                    break;
                case UserState.addPhotoToNote:
                    await botClient.SendTextMessageAsync(chatId, "В разработке", replyMarkup: GetKeyboard());
                    _userState[chatId] = UserState.idle;
                    break;
                case UserState.editTodo:
                    EditTodoForNoteAsync(messageText, chatId);
                    break;
                case UserState.selectNote:
                case UserState.idle:
                    ExecuteBotCommand(messageText, chatId);
                    break;
            }
        }

        private async Task EditTodoForNoteAsync(string messageText, long chatId) {
            throw new NotImplementedException();
        }

        private async Task EditNoteAsync(string messageText, long chatId) {
            throw new NotImplementedException();
        }

        private async Task SetNoteTextAsync(string messageText, long chatId) {
            if (!_lastNote.ContainsKey(chatId)) {
                await botClient.SendTextMessageAsync(chatId, "Ошибка", replyMarkup: GetKeyboard());
            }
            _lastNote[chatId].Content = messageText;
            try {
                _db.Notes.Add(_lastNote[chatId]);
                _db.SaveChanges();
                _lastNote.Remove(chatId);
            }
            catch (DbUpdateException ex) {
                _db.Notes.Remove(_lastNote[chatId]);
                _lastNote.Remove(chatId);
                await botClient.SendTextMessageAsync(chatId, ex.InnerException.Message);
            }
            _userState[chatId] = UserState.idle;
            await botClient.SendTextMessageAsync(chatId, "Готово!");
        }

        private async Task SetNoteTitleAsync(string messageText, long chatId) {
            if (!_lastNote.ContainsKey(chatId)) {
                await botClient.SendTextMessageAsync(chatId, "Ошибка", replyMarkup: GetKeyboard());
            }
            if (messageText == "⬅️ Назад" || messageText == "➡️ Вперед") {
                await botClient.SendTextMessageAsync(chatId, "Ошибка", replyMarkup: GetKeyboard());
            }
            _lastNote[chatId].Title = messageText;
            _userState[chatId] = UserState.createNoteText;
            await botClient.SendTextMessageAsync(chatId, "А теперь текст заметки:");
        }

        private async Task CreateNoteAsync(string messageText, long chatId) {
            await botClient.SendTextMessageAsync(chatId, "Какое будет название?");
            _userState[chatId] = UserState.createNoteTitle;
            if (!_lastNote.ContainsKey(chatId)) {
                _lastNote.Add(chatId, new() {
                    CreatedAt = DateTime.Now,
                    UserId = _db.Users.FirstOrDefault(x => x.TelegramId == chatId)?.Id,
                });
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken) {
            var ErrorMessage = exception switch {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private async Task RegisterUserAsync(long chatId) {

            if (_db.Users.FirstOrDefault(x => x.TelegramId == chatId) == null) {
                _db.Users.Add(new() { TelegramId = chatId });
                await _db.SaveChangesAsync();
                await botClient.SendTextMessageAsync(chatId, "Привет! Я бот для заметок. Чем могу помочь?", replyMarkup: GetKeyboard());

            }
            else {
                await botClient.SendTextMessageAsync(chatId, "Привет! Ты уже смешарик!", replyMarkup: GetKeyboard());
            }
            if (!_userState.ContainsKey(chatId)) {
                _userState.Add(chatId, UserState.idle);
            }
            if(!_userPage.ContainsKey(chatId)) {
                _userPage[chatId] = 0;
            }
        }

        private async Task ExecuteBotCommand(string messageText, long chatId) {
            if (_userState[chatId] == UserState.editNote) {
                switch (messageText) {
                    case "Удалить":
                        await DeleteNoteAsync(chatId);
                        break;
                    case "Изменить Название":
                        _userState[chatId] = UserState.editNoteTitle;
                        await botClient.SendTextMessageAsync(chatId, "Введите новое название:");
                        break;
                    case "Изменить Текст":
                        _userState[chatId] = UserState.editNoteText;
                        await botClient.SendTextMessageAsync(chatId, "Введите новый текст:");
                        break;
                    case "⬅️ Назад":
                        await botClient.SendTextMessageAsync(chatId, "Выберите заметку:", replyMarkup: GetNotes(chatId));
                        _userState[chatId] = UserState.selectNote;
                        break;
                }
                return;
            }

            if (_userState[chatId] == UserState.selectNote) {
                if (messageText == "⬅️ Назад" || messageText == "➡️ Вперед") {
                    await HandlePagination(messageText, chatId);
                    return;
                }
                var note = _db.Notes.FirstOrDefault(x => x.Title == messageText);
                if (note == null) {
                    await botClient.SendTextMessageAsync(chatId, "Заметка не найдена.", replyMarkup: GetKeyboard());
                    _userState[chatId] = UserState.idle;
                    return;
                }
                await ShowNoteAsync(chatId, note);
                _lastNote[chatId] = note;
                _userState[chatId] = UserState.editNote;
                return;
            }

            

            if (messageText == "Меню") {
                await botClient.SendTextMessageAsync(chatId, "Главное меню", replyMarkup: GetKeyboard());
                _userState[chatId] = UserState.idle;
                return;
            }

            

            

            switch (commandMappings[messageText]) {
                case Commands.start:
                    await RegisterUserAsync(chatId);
                    break;
                case Commands.createNote:
                    _userState[chatId] = UserState.createNote;
                    await GoToState(chatId, messageText);
                    break;
                case Commands.getNotes:
                    await botClient.SendTextMessageAsync(chatId, "Выберите заметку:", replyMarkup: GetNotes(chatId));
                    break;
                case Commands.getNote:
                    await ShowNoteAsync(chatId, _lastNote[chatId]);
                    break;
            }
        }

        private async Task HandlePagination(string messageText, long chatId) {
            if (messageText == "⬅️ Назад") {
                _userPage[chatId] = Math.Max(_userPage[chatId] - 1, 0);
            }
            else if (messageText == "➡️ Вперед") {
                _userPage[chatId] += 1;
            }

            await botClient.SendTextMessageAsync(chatId, "Выберите заметку:", replyMarkup: GetNotes(chatId));
        }

        private async Task ShowNoteAsync(long chatId, Note note) {
            var keyboardButtons = new List<KeyboardButton[]> {
                new KeyboardButton[] { new KeyboardButton("Изменить Текст"), new KeyboardButton("Изменить Название") },
                new KeyboardButton[] { new KeyboardButton("Удалить"), new KeyboardButton("⬅️ Назад") }
            };

            await botClient.SendTextMessageAsync(chatId, $"{note.Title}\n{note.Content}", replyMarkup: new ReplyKeyboardMarkup(keyboardButtons) {
                ResizeKeyboard = true
            });
        }

        private async Task DeleteNoteAsync(long chatId) {
            if (!_lastNote.ContainsKey(chatId)) {
                await botClient.SendTextMessageAsync(chatId, "Ошибка", replyMarkup: GetKeyboard());
                return;
            }
            _db.Notes.Remove(_lastNote[chatId]);
            await _db.SaveChangesAsync();
            _lastNote.Remove(chatId);
            await botClient.SendTextMessageAsync(chatId, "Заметка удалена.", replyMarkup: GetKeyboard());
            _userState[chatId] = UserState.idle;
        }

        private async Task EditNoteTitleAsync(long chatId, string newTitle) {
            if (!_lastNote.ContainsKey(chatId)) {
                await botClient.SendTextMessageAsync(chatId, "Ошибка", replyMarkup: GetKeyboard());
                return;
            }
            _lastNote[chatId].Title = newTitle;
            await _db.SaveChangesAsync();
            await botClient.SendTextMessageAsync(chatId, "Название изменено.", replyMarkup: GetKeyboard());
            _userState[chatId] = UserState.idle;
        }

        private async Task EditNoteTextAsync(long chatId, string newText) {
            if (!_lastNote.ContainsKey(chatId)) {
                await botClient.SendTextMessageAsync(chatId, "Ошибка", replyMarkup: GetKeyboard());
                return;
            }
            _lastNote[chatId].Content = newText;
            await _db.SaveChangesAsync();
            await botClient.SendTextMessageAsync(chatId, "Текст изменен.", replyMarkup: GetKeyboard());
            _userState[chatId] = UserState.idle;
        }

        private ReplyKeyboardMarkup GetNotes(long chatId) {
            _userState[chatId] = UserState.selectNote;
            int notesPerPage = 5;
            var notes = _db.Notes.Where(x => x.User.TelegramId == chatId).Skip(_userPage.GetValueOrDefault(chatId) * notesPerPage).Take(notesPerPage).ToList();
            var noteButtons = notes.Select(note => new KeyboardButton(note.Title)).ToArray();
            if(noteButtons.Length == 0) {
                _userPage[chatId] = _userPage[chatId] - 1;
                return GetNotes(chatId);
            }
            var keyboardButtons = new List<KeyboardButton[]> {
                noteButtons,
                new KeyboardButton[] { new KeyboardButton("⬅️ Назад"),  new KeyboardButton("➡️ Вперед") },
                new KeyboardButton[] { new KeyboardButton($"Страница: {_userPage[chatId]}")},
                new KeyboardButton[] { new KeyboardButton("Меню") }
            };

            return new ReplyKeyboardMarkup(keyboardButtons) { ResizeKeyboard = true };
        }

        private ReplyKeyboardMarkup GetKeyboard() {
            return new ReplyKeyboardMarkup(new[]{
                new KeyboardButton[] { "Добавить заметку", "Получить заметки" },
            }) {
                ResizeKeyboard = true
            };
        }

        private enum Commands {
            start,
            createNote,
            getNotes,
            getNote
        }

        private enum UserState {
            createNote,
            createNoteTitle,
            createNoteText,
            editNote,
            editNoteTitle,
            editNoteText,
            addPhotoToNote,
            editTodo,
            selectNote,
            idle,
        }
    }
    public interface ITelegramBot {
        Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken);
    }
}
