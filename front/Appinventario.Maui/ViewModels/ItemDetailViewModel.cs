using Appinventario.Models;
using Appinventario.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Appinventario.ViewModels
{
    [QueryProperty(nameof(ItemId), nameof(ItemId))]
    public class ItemDetailViewModel : BaseViewModel
    {
        private string itemId;
        private string ip;
        private string description;
        private bool porLote;
        public string Id { get; set; }

        public string IP
        {
            get => ip;
            set => SetProperty(ref ip, value);
        }
        public bool PorLote
        {
            get => porLote;
            set => SetProperty(ref porLote, value);
        }
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        public string ItemId
        {
            get
            {
                return itemId;
            }
            set
            {
                itemId = value;
                LoadItemId(value);
            }
        }

        public async void LoadItemId(string itemId)
        {
            try
            {
                var item = await DataStore.GetItemAsync(itemId);
                Id = item.Id;
                IP = item.IP;
                Description = item.Description;
            }
            catch (Exception)
            {
                Debug.WriteLine("Falla carga de item.");
            }
        }
    }
}

