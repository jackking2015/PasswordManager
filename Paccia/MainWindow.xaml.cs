﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Paccia
{
    public partial class MainWindow
    {
        readonly EncryptedRepositoryFactory<Secret> _repositoryFactory;
        IReadOnlyCollection<Secret> _readOnlyCollection;
        SecureString _passphrase;

        public MainWindow(EncryptedRepositoryFactory<Secret> repositoryFactory)
        {
            _repositoryFactory = repositoryFactory;

            InitializeComponent();
        }

        Repository<Secret> GetRepository(string passphrase) => _repositoryFactory.GetRepository(passphrase, Environment.MachineName, ConfigurationKey.SecretsFilePath);

        void MainWindowOnLoaded(object sender, EventArgs e) => MasterPasswordInputBox.Show();

        void EntryListViewOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var secrets = e.AddedItems.Cast<Secret>().ToArray();

            if (secrets.Length != 1)
                return;

            var secret = secrets.First();

            FieldsListView.ItemsSource = secret.Fields;
            SecretsListView.ItemsSource = secret.Secrets;
        }

        void EntrySearchTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (EntrySearchTextBox.Text.Length < 1)
            {
                EntryListView.ItemsSource = _readOnlyCollection;
                return;
            }

            var lower = EntrySearchTextBox.Text.ToLower();

            var itemsSource = _readOnlyCollection?.Where(secret => secret.Description.ToLower().Contains(lower));

            if (EntryListView != null)
                EntryListView.ItemsSource = itemsSource;
        }

        async void AddSecretButtonOnClick(object sender, RoutedEventArgs e)
        {
            MainContent.IsEnabled = false;
            
            var secret = await AddSecretBox.CreateSecretAsync();

            if (secret != null)
            {
                _readOnlyCollection = _readOnlyCollection.Concat(new[] { secret }).ToArray();

                await GetRepository(_passphrase.ToClearString()).SaveAsync(_readOnlyCollection);

                EntryListView.ItemsSource = _readOnlyCollection;
            }

            MainContent.IsEnabled = true;
        }

        void ShowSecretButtonOnClick(object sender, RoutedEventArgs e) => SecretTextBox.Visibility = Visibility.Visible;

        void CopySecretButtonOnClick(object sender, RoutedEventArgs e)
        {
            var item = FieldsListView.SelectedItem ?? SecretsListView.SelectedItem;

            var selectedItem = (KeyValuePair<string, string>)item;

            Clipboard.SetText(SecretTextBox.Text);

            Title = $"Copied {selectedItem.Key}";
        }

        void FieldsListViewOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1)
                return;

            SecretsListView.SelectedItem = null;

            var selectedItem = FieldsListView.SelectedItem as KeyValuePair<string, string>?;

            SecretTextBox.Text = selectedItem?.Value;

            SecretTextBox.Visibility = Visibility.Visible;
        }

        void SecretsListViewOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1)
                return;

            FieldsListView.SelectedItem = null;

            var selectedItem = SecretsListView.SelectedItem as KeyValuePair<string, string>?;

            SecretTextBox.Visibility = Visibility.Hidden;

            SecretTextBox.Text = selectedItem?.Value;
        }
        
        async Task LoadAndPopulateList(SecureString passphrase)
        {
            // Use keylogger prevention.
            // Maybe: 1-3-5 character of the passphrase first
            // then 2-4-6...
            _passphrase = passphrase;

            IsEnabled = false;

            _readOnlyCollection = await GetRepository(_passphrase.ToClearString()).LoadAsync();

            EntryListView.ItemsSource = _readOnlyCollection;

            IsEnabled = true;
        }
        
        async void MasterPasswordInputBoxPasswordInserted(object sender, SecureString e) => await LoadAndPopulateList(e);

        void MasterPasswordInputBoxOnCancel(object sender, EventArgs e) => Close();
    }
}
