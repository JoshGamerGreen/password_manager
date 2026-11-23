using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PasswordManager.Core.Exceptions;
using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;
using PasswordManager.Core.Services;

namespace PasswordManager.App;

public partial class MainWindow : Window
{
    private readonly IVaultRepository _repository;
    private readonly VaultService _service;
    private readonly InactivityMonitor _monitor;
    private readonly ClipboardService _clipboard;
    private readonly IPasswordGenerator _generator = new RandomPasswordGenerator();
    private readonly IStrengthEvaluator _evaluator = new BasicStrengthEvaluator();
    private readonly DispatcherTimer _lockWatcher;

    private Guid? _editingId; // null = entrada nueva

    public MainWindow()
    {
        // La bóveda se guarda en %APPDATA%\PasswordManager\vault.dat
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PasswordManager");
        _repository = new FileVaultRepository(Path.Combine(folder, "vault.dat"));

        // Tiempo de inactividad antes del bloqueo automático
        _monitor = new InactivityMonitor(TimeSpan.FromMinutes(2));
        _service = new VaultService(new AesGcmCryptoService(), _repository, monitor: _monitor);

        // El portapapeles de Windows solo se toca desde el hilo de la ventana,
        // por eso cada acción pasa por Dispatcher.Invoke
        _clipboard = new ClipboardService(
            () => Dispatcher.Invoke(() =>
            {
                try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
                catch { return null; }
            }),
            text => Dispatcher.Invoke(() =>
            {
                try { Clipboard.SetText(text); } catch { }
            }),
            () => Dispatcher.Invoke(() =>
            {
                try { Clipboard.Clear(); } catch { }
            }));

        InitializeComponent();

        // El monitor bloquea la bóveda desde otro hilo y no puede tocar la pantalla.
        // Este reloj revisa cada segundo si se bloqueó, para volver al login.
        _lockWatcher = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _lockWatcher.Tick += (_, _) =>
        {
            if (!_service.IsUnlocked && LoginPanel.Visibility != Visibility.Visible)
                ShowLogin();
        };
        _lockWatcher.Start();

        Closed += (_, _) =>
        {
            _lockWatcher.Stop();
            _service.Lock();
            _clipboard.Dispose();
            _monitor.Dispose();
        };

        ShowLogin();
    }

    // ================= NAVEGACIÓN ENTRE PANTALLAS =================

    private void ShowPanel(UIElement panel)
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        ListPanel.Visibility = Visibility.Collapsed;
        EditorPanel.Visibility = Visibility.Collapsed;
        panel.Visibility = Visibility.Visible;
    }

    private void ShowLogin()
    {
        // Limpiar de la pantalla todo lo sensible
        EntriesList.ItemsSource = null;
        SearchBox.Text = "";
        ClearEditor();
        MasterBox.Clear();
        ConfirmBox.Clear();
        LoginError.Text = "";
        StatusText.Text = "";

        bool exists = _repository.Exists();
        ConfirmPanel.Visibility = exists ? Visibility.Collapsed : Visibility.Visible;
        LoginButton.Content = exists ? "Desbloquear" : "Crear bóveda";
        LoginSubtitle.Text = exists
            ? "Ingresa tu contraseña maestra."
            : "Crea una contraseña maestra para tu bóveda nueva. Si la olvidas, no se puede recuperar.";

        ShowPanel(LoginPanel);
        MasterBox.Focus();
    }

    // Cualquier tecla o clic cuenta como actividad: reinicia el bloqueo automático
    private void Window_Activity(object sender, RoutedEventArgs e) => _monitor.Reset();

    // Ejecuta una acción; si la bóveda se bloqueó mientras tanto, vuelve al login
    private void Run(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { ShowLogin(); }
        catch (Exception ex) { StatusText.Text = ex.Message; }
    }

    // ================= LOGIN =================

    private void MasterBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Login_Click(sender, e);
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        LoginError.Text = "";
        try
        {
            if (_repository.Exists())
            {
                _service.Unlock(MasterBox.Password);
            }
            else
            {
                if (MasterBox.Password.Length < 8)
                {
                    LoginError.Text = "Usa al menos 8 caracteres.";
                    return;
                }
                if (MasterBox.Password != ConfirmBox.Password)
                {
                    LoginError.Text = "Las contraseñas no coinciden.";
                    return;
                }
                _service.CreateVault(MasterBox.Password);
            }
        }
        catch (InvalidMasterPasswordException)
        {
            LoginError.Text = "Contraseña incorrecta.";
            MasterBox.Clear();
            return;
        }
        catch (CorruptedVaultException)
        {
            LoginError.Text = "El archivo de la bóveda está dañado.";
            return;
        }
        catch (Exception ex)
        {
            LoginError.Text = ex.Message;
            return;
        }

        MasterBox.Clear();
        ConfirmBox.Clear();
        ShowPanel(ListPanel);
        RefreshList();
    }

    // ================= LISTA =================

    private void RefreshList()
    {
        if (!_service.IsUnlocked) return;
        try
        {
            EntriesList.ItemsSource = _service.Search(SearchBox.Text)
                                              .OrderBy(x => x.Title)
                                              .ToList();
        }
        catch (InvalidOperationException) { ShowLogin(); }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshList();

    private void Lock_Click(object sender, RoutedEventArgs e)
    {
        _service.Lock();
        ShowLogin();
    }

    private void Add_Click(object sender, RoutedEventArgs e) => OpenEditor(null);

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesList.SelectedItem is not VaultEntry entry)
        {
            StatusText.Text = "Selecciona una entrada primero.";
            return;
        }
        OpenEditor(entry);
    }

    private void EntriesList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EntriesList.SelectedItem is VaultEntry entry) OpenEditor(entry);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesList.SelectedItem is not VaultEntry entry)
        {
            StatusText.Text = "Selecciona una entrada primero.";
            return;
        }

        var respuesta = MessageBox.Show($"¿Eliminar \"{entry.Title}\"?", "Confirmar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (respuesta != MessageBoxResult.Yes) return;

        Run(() =>
        {
            _service.DeleteEntry(entry.Id);
            StatusText.Text = "Entrada eliminada.";
            RefreshList();
        });
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesList.SelectedItem is not VaultEntry selected)
        {
            StatusText.Text = "Selecciona una entrada primero.";
            return;
        }

        Run(() =>
        {
            // Pedirla al servicio comprueba que la bóveda sigue abierta
            var entry = _service.GetEntry(selected.Id);
            if (entry == null) return;

            _clipboard.CopyWithAutoClear(entry.Password, 30);
            StatusText.Text = $"Contraseña de \"{entry.Title}\" copiada. Se borrará en 30 segundos.";
        });
    }

    // ================= EDITOR =================

    private void OpenEditor(VaultEntry? entry)
    {
        _editingId = entry?.Id;
        EditorTitle.Text = entry == null ? "Nueva entrada" : "Editar entrada";
        TitleBox.Text = entry?.Title ?? "";
        UserBox.Text = entry?.Username ?? "";
        UrlBox.Text = entry?.Url ?? "";
        CategoryBox.Text = entry?.Category ?? "";
        NotesBox.Text = entry?.Notes ?? "";
        EntryPassBox.Text = entry?.Password ?? "";
        EditorError.Text = "";

        ShowPanel(EditorPanel);
        TitleBox.Focus();
    }

    private void ClearEditor()
    {
        _editingId = null;
        TitleBox.Text = "";
        UserBox.Text = "";
        UrlBox.Text = "";
        CategoryBox.Text = "";
        NotesBox.Text = "";
        EntryPassBox.Text = "";
        EditorError.Text = "";
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        EntryPassBox.Text = _generator.Generate(
            new PasswordOptions { Length = (int)LengthSlider.Value });
    }

    private void EntryPassBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (EntryPassBox.Text.Length == 0)
        {
            StrengthText.Text = "";
            return;
        }

        var (etiqueta, color) = _evaluator.Evaluate(EntryPassBox.Text) switch
        {
            PasswordStrength.VeryWeak => ("Muy débil", Brushes.Firebrick),
            PasswordStrength.Weak => ("Débil", Brushes.OrangeRed),
            PasswordStrength.Medium => ("Media", Brushes.DarkOrange),
            PasswordStrength.Strong => ("Fuerte", Brushes.SeaGreen),
            _ => ("Muy fuerte", Brushes.DarkGreen)
        };

        StrengthText.Text = etiqueta;
        StrengthText.Foreground = color;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            EditorError.Text = "El título es obligatorio.";
            return;
        }
        if (string.IsNullOrEmpty(EntryPassBox.Text))
        {
            EditorError.Text = "La contraseña es obligatoria.";
            return;
        }

        try
        {
            if (_editingId == null)
            {
                _service.AddEntry(new VaultEntry
                {
                    Title = TitleBox.Text.Trim(),
                    Username = UserBox.Text.Trim(),
                    Url = UrlBox.Text.Trim(),
                    Category = CategoryBox.Text.Trim(),
                    Notes = NotesBox.Text,
                    Password = EntryPassBox.Text
                });
            }
            else
            {
                var entry = _service.GetEntry(_editingId.Value);
                if (entry == null)
                {
                    EditorError.Text = "La entrada ya no existe.";
                    return;
                }

                entry.Title = TitleBox.Text.Trim();
                entry.Username = UserBox.Text.Trim();
                entry.Url = UrlBox.Text.Trim();
                entry.Category = CategoryBox.Text.Trim();
                entry.Notes = NotesBox.Text;

                // UpdatePassword actualiza la fecha de cambio, solo si cambió
                if (entry.Password != EntryPassBox.Text)
                    entry.UpdatePassword(EntryPassBox.Text);

                _service.UpdateEntry(entry);
            }
        }
        catch (InvalidOperationException) { ShowLogin(); return; } // se bloqueó mientras editabas
        catch (Exception ex) { EditorError.Text = ex.Message; return; }

        CloseEditor();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => CloseEditor();

    private void CloseEditor()
    {
        ClearEditor();
        ShowPanel(ListPanel);
        RefreshList();
    }
}