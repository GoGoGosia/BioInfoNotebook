using BioInfoNotebook.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BioInfoNotebook.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dbPath = Path.Combine(basePath, "BioInfoNotebookBaza.db3");

            _database = new SQLiteAsyncConnection(dbPath);
            InitializeDatabaseAsync();
        }

        private async void InitializeDatabaseAsync()
        {
            await _database.CreateTableAsync<SesjaAnalityczna>();
            await _database.CreateTableAsync<WpisSesji>();
            await _database.CreateTableAsync<ZalacznikPliku>();
        }

        // OPERACJE NA SESJACH
        public async Task<List<SesjaAnalityczna>> PobierzWszystkieSesjeAsync()
        {
            return await _database.Table<SesjaAnalityczna>().OrderByDescending(s => s.DataUtworzenia).ToListAsync();
        }

        public async Task<int> ZapiszSesjeAsync(SesjaAnalityczna sesja)
        {
            if (sesja.Id != 0) return await _database.UpdateAsync(sesja);
            return await _database.InsertAsync(sesja);
        }

        public async Task UsunSesjeKaskadowoAsync(int sesjaId)
        {
            var wpisy = await PobierzWpisyDlaSesjiAsync(sesjaId);
            foreach (var wpis in wpisy)
            {
                await _database.Table<ZalacznikPliku>().Where(z => z.WpisId == wpis.Id).DeleteAsync();
                await _database.DeleteAsync(wpis);
            }
            await _database.Table<SesjaAnalityczna>().Where(s => s.Id == sesjaId).DeleteAsync();
        }

        // OPERACJE NA WPISACH
        public async Task<List<WpisSesji>> PobierzWpisyDlaSesjiAsync(int sesjaId)
        {
            return await _database.Table<WpisSesji>().Where(w => w.SesjaId == sesjaId).OrderBy(w => w.DataDodania).ToListAsync();
        }

        public async Task<int> ZapiszWpisAsync(WpisSesji wpis)
        {
            if (wpis.Id != 0) return await _database.UpdateAsync(wpis);
            return await _database.InsertAsync(wpis);
        }

        public async Task UsunWpisAsync(int wpisId)
        {
            await _database.Table<ZalacznikPliku>().Where(z => z.WpisId == wpisId).DeleteAsync();
            await _database.Table<WpisSesji>().Where(w => w.Id == wpisId).DeleteAsync();
        }

        // OPERACJE NA ZAŁĄCZNIKACH 
        public async Task<List<ZalacznikPliku>> PobierzZalacznikiDlaWpisuAsync(int wpisId)
        {
            return await _database.Table<ZalacznikPliku>().Where(z => z.WpisId == wpisId).ToListAsync();
        }

        public async Task<int> DodajZalacznikAsync(ZalacznikPliku zalacznik)
        {
            return await _database.InsertAsync(zalacznik);
        }
    }
}