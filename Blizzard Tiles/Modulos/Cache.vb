﻿Imports Windows.Networking.BackgroundTransfer
Imports Windows.Storage
Imports Windows.Storage.FileProperties

Module Cache

    Public Sub Cargar()

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim cbActivar As CheckBox = pagina.FindName("cbActivarCache")
        Dim spOpciones As StackPanel = pagina.FindName("spCacheOpciones")

        RemoveHandler cbActivar.Checked, AddressOf ActivarCache
        AddHandler cbActivar.Checked, AddressOf ActivarCache

        RemoveHandler cbActivar.Unchecked, AddressOf ActivarCache
        AddHandler cbActivar.Unchecked, AddressOf ActivarCache

        Dim botonLimpiar As Button = pagina.FindName("botonConfigLimpiarCache")

        RemoveHandler botonLimpiar.Click, AddressOf Limpiar
        AddHandler botonLimpiar.Click, AddressOf Limpiar

        If Not ApplicationData.Current.LocalSettings.Values("cache") = Nothing Then
            If ApplicationData.Current.LocalSettings.Values("cache") = 0 Then
                cbActivar.IsChecked = False
                spOpciones.Visibility = Visibility.Collapsed
            Else
                cbActivar.IsChecked = True
                spOpciones.Visibility = Visibility.Visible
            End If
        Else
            ApplicationData.Current.LocalSettings.Values("cache") = 0
            spOpciones.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Sub ActivarCache(sender As Object, e As RoutedEventArgs)

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim cb As CheckBox = sender
        Dim spOpciones As StackPanel = pagina.FindName("spCacheOpciones")

        If cb.IsChecked = False Then
            ApplicationData.Current.LocalSettings.Values("cache") = 0
            spOpciones.Visibility = Visibility.Collapsed
        Else
            ApplicationData.Current.LocalSettings.Values("cache") = 1
            spOpciones.Visibility = Visibility.Visible
        End If

    End Sub

    Public Async Function DescargarImagen(enlace As String, id As String, tipo As String) As Task(Of String)

        Dim imagenFinal As String = String.Empty

        If ApplicationData.Current.LocalSettings.Values("cache") = 1 Then
            If Not enlace = String.Empty Then
                Dim carpetaImagenes As StorageFolder = Nothing

                If Directory.Exists(ApplicationData.Current.LocalFolder.Path + "\Cache") = False Then
                    carpetaImagenes = Await ApplicationData.Current.LocalFolder.CreateFolderAsync("Cache")
                Else
                    carpetaImagenes = Await StorageFolder.GetFolderFromPathAsync(ApplicationData.Current.LocalFolder.Path + "\Cache")
                End If

                If Not carpetaImagenes Is Nothing Then
                    If Not File.Exists(ApplicationData.Current.LocalFolder.Path + "\Cache\" + id + tipo) Then
                        Dim ficheroImagen As IStorageFile = Nothing

                        Try
                            ficheroImagen = Await carpetaImagenes.CreateFileAsync(id + tipo, CreationCollisionOption.ReplaceExisting)
                        Catch ex As Exception

                        End Try

                        If Not ficheroImagen Is Nothing Then
                            Dim descargador As New BackgroundDownloader
                            Dim descarga As DownloadOperation = descargador.CreateDownload(New Uri(enlace), ficheroImagen)
                            descarga.Priority = BackgroundTransferPriority.High
                            Await descarga.StartAsync

                            If descarga.Progress.Status = BackgroundTransferStatus.Completed Then
                                Dim ficheroDescargado As IStorageFile = descarga.ResultFile

                                imagenFinal = ficheroDescargado.Path
                            End If
                        End If
                    Else
                        Dim ficheroImagen As IStorageFile = Await StorageFile.GetFileFromPathAsync(ApplicationData.Current.LocalFolder.Path + "\Cache\" + id + tipo)
                        Dim tamaño As BasicProperties = Await ficheroImagen.GetBasicPropertiesAsync

                        If tamaño.Size = 0 Then
                            imagenFinal = Nothing
                        Else
                            imagenFinal = ApplicationData.Current.LocalFolder.Path + "\Cache\" + id + tipo
                        End If
                    End If
                Else
                    imagenFinal = enlace
                End If
            End If
        Else
            imagenFinal = enlace
        End If

        Return imagenFinal

    End Function

    Public Async Sub Limpiar(sender As Object, e As RoutedEventArgs)

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim boton As Button = pagina.FindName("botonConfigLimpiarCache")
        boton.IsEnabled = False

        Dim pr As ProgressRing = pagina.FindName("prConfigLimpiarCache")
        pr.Visibility = Visibility.Visible

        Dim gridSeleccionarJuego As Grid = pagina.FindName("gridSeleccionarJuego")
        gridSeleccionarJuego.Visibility = Visibility.Collapsed

        If File.Exists(ApplicationData.Current.LocalFolder.Path + "\juegos") Then
            File.Delete(ApplicationData.Current.LocalFolder.Path + "\juegos")
        End If

        If Directory.Exists(ApplicationData.Current.LocalFolder.Path + "\Cache") = True Then
            Dim carpetaImagenes As StorageFolder = Await StorageFolder.GetFolderFromPathAsync(ApplicationData.Current.LocalFolder.Path + "\Cache")

            If Not carpetaImagenes Is Nothing Then
                Await carpetaImagenes.DeleteAsync
            End If
        End If

        Blizzard.Generar()

        pr.Visibility = Visibility.Collapsed

    End Sub

    Public Sub Estado(estado As Boolean)

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim cbActivar As CheckBox = pagina.FindName("cbActivarCache")
        cbActivar.IsEnabled = estado

        Dim botonLimpiar As Button = pagina.FindName("botonConfigLimpiarCache")
        botonLimpiar.IsEnabled = estado

    End Sub

End Module