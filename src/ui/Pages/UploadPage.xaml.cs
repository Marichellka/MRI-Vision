﻿using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MRI_Vision.UI.Pages;

public partial class UploadPage : Page
{
    public UploadPage()
    {
        InitializeComponent();
    }
    
    public void OpenExplorerButton_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        OpenFileDialog fileDialog= new OpenFileDialog(); 
        fileDialog.DefaultExt = ".nii.gz"; // Required file extension 
        fileDialog.Filter = "Compressed archive folder|*.nii.gz";

        if (fileDialog.ShowDialog() is true)
        {
            // Redirect to next page
            // NavigationService!.Navigate();
        }
    }
    
    private void DropBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;

            if (files.Length != 1)
            {
                MessageBox.Show("You can have only one file");
                return;
            }

            string filePath = files[0];

            if (filePath.Substring(filePath.Length-4)!=".nii.gz")
            {
                MessageBox.Show("File extension should be .nii.gz");
                return;
            }
            
            // Redirect to next page
            // NavigationService!.Navigate();
        }
    }
}