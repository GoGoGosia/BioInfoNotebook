using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BioInfoNotebook.Models;
using BioInfoNotebook.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BioInfoNotebook.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly DatabaseService _dbService;

        // Lista sesji widoczna w lewym panelu bocznym
        [ObservableProperty]
        private ObservableCollection<SesjaAnalityczna> _widoczneSesje = new();

        // Obecnie kliknięta/wybrana sesja przez biologa
        [ObservableProperty]
        private SesjaAnalityczna? _wybranaSesja;

        // Pole tekstowe do wpisywania tytułu nowej sesji
        [ObservableProperty]
        private string _nowyTytulSesji = string.Empty;

        // Dynamiczna lista notatek laboratoryjnych powiązanych z aktywną sesją
        [ObservableProperty]
        private ObservableCollection<WpisDisplayModel> _wpisyAktywnejSesji = new();

        // Pole tekstowe nowej notatki laboratoryjnej
        [ObservableProperty]
        private string _nowaTrescWpisu = string.Empty;

        // Właściwość sprawdzająca, czy przyciski edycji mają być aktywne
        public bool CzySesjaWybrana => WybranaSesja != null;

        public MainWindowViewModel()
        {
            _dbService = new DatabaseService();

            try
            {
                var propertyLicencji = typeof(QuestPDF.Settings).GetProperty("License");
                if (propertyLicencji != null)
                {
                    var typEnuma = propertyLicencji.PropertyType;
                    var wartoscCommunity = System.Enum.Parse(typEnuma, "Community");
                    propertyLicencji.SetValue(null, wartoscCommunity);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd licencji PDF: {ex.Message}");
            }

            _ = ZaladujSesjeAsync();
        }

        partial void OnWybranaSesjaChanged(SesjaAnalityczna? value)
        {
            OnPropertyChanged(nameof(CzySesjaWybrana));
            _ = ZaladujWpisyAsync();
        }

        public async Task ZaladujSesjeAsync()
        {
            var sesje = await _dbService.PobierzWszystkieSesjeAsync();
            WidoczneSesje = new ObservableCollection<SesjaAnalityczna>(sesje);
        }

        public async Task ZaladujWpisyAsync()
        {
            if (WybranaSesja == null)
            {
                WpisyAktywnejSesji.Clear();
                return;
            }

            var wpisy = await _dbService.PobierzWpisyDlaSesjiAsync(WybranaSesja.Id);
            var listaWyswietlania = new ObservableCollection<WpisDisplayModel>();

            foreach (var wpis in wpisy)
            {
                var zalaczniki = await _dbService.PobierzZalacznikiDlaWpisuAsync(wpis.Id);

                listaWyswietlania.Add(new WpisDisplayModel
                {
                    Id = wpis.Id,
                    SesjaId = wpis.SesjaId,
                    TrescWpisu = wpis.TrescWpisu,
                    DataDodania = wpis.DataDodania,
                    Zalaczniki = new ObservableCollection<ZalacznikPliku>(zalaczniki)
                });
            }

            WpisyAktywnejSesji = listaWyswietlania;
        }

        [RelayCommand]
        private async Task DodajSesje()
        {
            if (string.IsNullOrWhiteSpace(NowyTytulSesji)) return;

            var nowaSesja = new SesjaAnalityczna
            {
                TytulSesji = NowyTytulSesji,
                DataUtworzenia = DateTime.Now
            };

            await _dbService.ZapiszSesjeAsync(nowaSesja);
            NowyTytulSesji = string.Empty;
            await ZaladujSesjeAsync();
        }

        [RelayCommand]
        private async Task UsunSesje()
        {
            if (WybranaSesja == null) return;

            await _dbService.UsunSesjeKaskadowoAsync(WybranaSesja.Id);
            WybranaSesja = null;
            await ZaladujSesjeAsync();
        }

        [RelayCommand]
        private async Task DodajWpis()
        {
            if (WybranaSesja == null || string.IsNullOrWhiteSpace(NowaTrescWpisu)) return;

            var nowyWpis = new WpisSesji
            {
                SesjaId = WybranaSesja.Id,
                TrescWpisu = NowaTrescWpisu,
                DataDodania = DateTime.Now
            };

            await _dbService.ZapiszWpisAsync(nowyWpis);
            NowaTrescWpisu = string.Empty;
            await ZaladujWpisyAsync();
        }

        [RelayCommand]
        private async Task UsunWpis(WpisDisplayModel wpis)
        {
            if (wpis == null) return;
            await _dbService.UsunWpisAsync(wpis.Id);
            await ZaladujWpisyAsync();
        }

        [RelayCommand]
        private async Task DodajZalacznik(WpisDisplayModel wpis)
        {
            if (wpis == null) return;

            var desktop = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var topLevel = TopLevel.GetTopLevel(desktop?.MainWindow);
            if (topLevel == null) return;

            var pliki = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Wybierz załącznik sekwencji lub wykresu",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Dane BioInfo (*.fasta, *.csv, *.png)")
                    {
                        Patterns = new[] { "*.fasta", "*.fa", "*.csv", "*.png" }
                    }
                }
            });

            if (pliki.Count > 0)
            {
                var plik = pliki[0];
                var sciezkaLokalna = plik.Path.LocalPath;
                var nazwa = plik.Name;
                var rozszerzenie = Path.GetExtension(nazwa).ToUpper().Replace(".", "");

                var folderAplikacji = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BioInfoNotebook_Pliki");
                Directory.CreateDirectory(folderAplikacji);

                var unikalnaNazwa = Guid.NewGuid().ToString() + "_" + nazwa;
                var sciezkaDocelowa = Path.Combine(folderAplikacji, unikalnaNazwa);

                File.Copy(sciezkaLokalna, sciezkaDocelowa, true);

                var nowyZalacznik = new ZalacznikPliku
                {
                    WpisId = wpis.Id,
                    NazwaPliku = nazwa,
                    SciezkaKopii = sciezkaDocelowa,
                    TypRozszerzenia = rozszerzenie
                };

                await _dbService.DodajZalacznikAsync(nowyZalacznik);
                await ZaladujWpisyAsync();
            }
        }

        [RelayCommand]
        private async Task EksportujPdf()
        {
            if (WybranaSesja == null) return;

            var desktop = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var topLevel = TopLevel.GetTopLevel(desktop?.MainWindow);
            if (topLevel == null) return;

            var plikOcalenia = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Zapisz raport końcowy PDF",
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Dokument PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (plikOcalenia != null)
            {
                var sciezkaPdf = plikOcalenia.Path.LocalPath;
                var tytulSesji = WybranaSesja.TytulSesji;
                var dataSesji = WybranaSesja.DataUtworzenia;
                var kopiaWpisow = WpisyAktywnejSesji;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(txt => txt.FontSize(11).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            col.Item().Text("RAPORT Z SESJI BIOINFORMATYCZNEJ")
                                .FontSize(20).Bold().FontColor(Colors.Green.Darken3);
                            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                            col.Item().PaddingTop(6).Text($"Temat: {tytulSesji}").FontSize(14).Bold();

                            col.Item().Text($"Wygenerowano: {dataSesji:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingBottom(15);
                        });

                        page.Content().Column(col =>
                        {
                            foreach (var wpis in kopiaWpisow)
                            {
                                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(wCol =>
                                {
                                    wCol.Item().Text($"{wpis.DataDodania:dd.MM.yyyy HH:mm:ss}").FontSize(9).Bold().FontColor(Colors.Green.Medium);
                                    wCol.Item().PaddingTop(4).Text(wpis.TrescWpisu).FontSize(12);

                                    if (wpis.Zalaczniki.Count > 0)
                                    {
                                        wCol.Item().PaddingTop(8).Text("Powiązane załączniki laboratoryjne:").FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                                        foreach (var zal in wpis.Zalaczniki)
                                        {
                                            wCol.Item().PaddingLeft(10).Text($"• {zal.NazwaPliku} [Format: {zal.TypRozszerzenia}]").FontSize(10);
                                        }
                                    }
                                });
                                col.Item().PaddingBottom(12);
                            }
                        });

                        page.Footer().AlignCenter().Text(t =>
                        {
                            t.CurrentPageNumber();
                            t.Span(" / ");
                            t.TotalPages();
                        });
                    });
                }).GeneratePdf(sciezkaPdf);
            }
        }
    }

    public class WpisDisplayModel
    {
        public int Id { get; set; }
        public int SesjaId { get; set; }
        public string TrescWpisu { get; set; } = string.Empty;
        public DateTime DataDodania { get; set; }
        public ObservableCollection<ZalacznikPliku> Zalaczniki { get; set; } = new();
    }
}