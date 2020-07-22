﻿Imports Microsoft.Toolkit.Uwp.Helpers
Imports Microsoft.Toolkit.Uwp.UI.Animations
Imports Microsoft.Toolkit.Uwp.UI.Controls
Imports Newtonsoft.Json
Imports Windows.Storage
Imports Windows.UI
Imports Windows.UI.Core
Imports Windows.UI.Xaml.Media.Animation

Module Blizzard

    Public anchoColumna As Integer = 232

    Public Async Sub Generar()

        Dim helper As New LocalObjectStorageHelper

        Dim recursos As New Resources.ResourceLoader()

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim spProgreso As StackPanel = pagina.FindName("spProgreso")
        spProgreso.Visibility = Visibility.Visible

        Dim pbProgreso As ProgressBar = pagina.FindName("pbProgreso")
        pbProgreso.Value = 0

        Dim tbProgreso As TextBlock = pagina.FindName("tbProgreso")
        tbProgreso.Text = String.Empty

        Dim botonCache As Button = pagina.FindName("botonConfigLimpiarCache")
        botonCache.IsEnabled = False

        Dim gridSeleccionarJuego As Grid = pagina.FindName("gridSeleccionarJuego")
        gridSeleccionarJuego.Visibility = Visibility.Collapsed

        Dim gv As AdaptiveGridView = pagina.FindName("gvTiles")
        gv.Items.Clear()

        Dim listaJuegos As New List(Of Tile)

        If Await helper.FileExistsAsync("juegos") = True Then
            listaJuegos = Await helper.ReadFileAsync(Of List(Of Tile))("juegos")
        End If

        Dim listaFamilias As List(Of String) = BlizzardBBDD.Familias
        Dim listaFamilias2 As New List(Of BlizzardAPIFamilia)

        Dim k As Integer = 0
        For Each familia In listaFamilias
            Dim html As String = Await Decompiladores.HttpClient(New Uri("https://eu.shop.battle.net/api/browsing/family/" + familia))

            If Not html = Nothing Then
                Dim listaJuegosFamilia As BlizzardAPIFamilia = JsonConvert.DeserializeObject(Of BlizzardAPIFamilia)(html)
                listaFamilias2.Add(listaJuegosFamilia)
            End If

            pbProgreso.Value = CInt((100 / listaFamilias.Count) * k)
            tbProgreso.Text = k.ToString + "/" + listaFamilias.Count.ToString
            k += 1
        Next

        Dim juegosBBDD As List(Of BlizzardJuego) = BlizzardBBDD.IDs

        For Each juegoBBDD In juegosBBDD
            For Each familia2 In listaFamilias2
                Dim añadir As Boolean = False

                For Each ficha In familia2.Fichas
                    Dim idTienda As String = juegoBBDD.IDTienda

                    For Each id In ficha.IDs
                        If id = idTienda Then
                            añadir = True
                        End If
                    Next

                    Dim i As Integer = 0
                    If Not listaJuegos Is Nothing Then
                        While i < listaJuegos.Count
                            If listaJuegos(i).ID = idTienda Then
                                añadir = False
                            End If
                            i += 1
                        End While
                    End If

                    If añadir = True Then
                        Dim titulo As String = ficha.Titulo

                        Dim logo As String = String.Empty

                        If juegoBBDD.MostrarLogo = True Then
                            Dim htmlLogo As String = Await Decompiladores.HttpClient(New Uri("https://eu.shop.battle.net/api/product/" + juegoBBDD.Slug))

                            If Not htmlLogo = Nothing Then
                                Dim juegoLogo As BlizzardAPIJuego = JsonConvert.DeserializeObject(Of BlizzardAPIJuego)(htmlLogo)
                                logo = juegoLogo.Icono
                            End If

                            If Not logo = String.Empty Then
                                If Not logo.Contains("https:") Then
                                    logo = "https:" + logo
                                End If
                            End If
                        End If

                        Dim horizontal As String = Await Cache.DescargarImagen(ficha.ImagenHorizontal, juegoBBDD.IDTienda, "horizontal")

                        If Not horizontal = String.Empty Then
                            If Not horizontal.Contains("https:") Then
                                horizontal = "https:" + horizontal
                            End If
                        End If

                        Dim vertical As String = Await Cache.DescargarImagen(ficha.ImagenVertical, juegoBBDD.IDTienda, "vertical")

                        If Not vertical = String.Empty Then
                            If Not vertical.Contains("https:") Then
                                vertical = "https:" + vertical
                            End If
                        End If

                        Dim juego As New Tile(titulo, idTienda, "battlenet://" + juegoBBDD.IDEjecutable, logo, logo, logo, horizontal, vertical)
                        listaJuegos.Add(juego)
                        Exit For
                    End If
                Next

                If añadir = True Then
                    Exit For
                End If
            Next
        Next

        spProgreso.Visibility = Visibility.Collapsed

        Await helper.SaveFileAsync(Of List(Of Tile))("juegos", listaJuegos)

        Dim gridTiles As Grid = pagina.FindName("gridTiles")
        Dim gridAvisoNoJuegos As Grid = pagina.FindName("gridAvisoNoJuegos")
        Dim spBuscador As StackPanel = pagina.FindName("spBuscador")

        If Not listaJuegos Is Nothing Then
            If listaJuegos.Count > 0 Then
                gridTiles.Visibility = Visibility.Visible
                gridAvisoNoJuegos.Visibility = Visibility.Collapsed
                gridSeleccionarJuego.Visibility = Visibility.Visible
                spBuscador.Visibility = Visibility.Visible

                listaJuegos.Sort(Function(x, y) x.Titulo.CompareTo(y.Titulo))

                gv.Items.Clear()

                For Each juego In listaJuegos
                    BotonEstilo(juego, gv)
                Next
            Else
                gridTiles.Visibility = Visibility.Collapsed
                gridAvisoNoJuegos.Visibility = Visibility.Visible
                gridSeleccionarJuego.Visibility = Visibility.Collapsed
                spBuscador.Visibility = Visibility.Collapsed

                gv.Visibility = Visibility.Collapsed
            End If
        Else
            gridTiles.Visibility = Visibility.Collapsed
            gridAvisoNoJuegos.Visibility = Visibility.Visible
            gridSeleccionarJuego.Visibility = Visibility.Collapsed
            spBuscador.Visibility = Visibility.Collapsed

            gv.Visibility = Visibility.Collapsed
        End If

        botonCache.IsEnabled = True

    End Sub

    Public Sub BotonEstilo(juego As Tile, gv As GridView)

        Dim panel As New DropShadowPanel With {
            .Margin = New Thickness(10, 10, 10, 10),
            .ShadowOpacity = 0.9,
            .BlurRadius = 10,
            .MaxWidth = anchoColumna + 20,
            .HorizontalAlignment = HorizontalAlignment.Center,
            .VerticalAlignment = VerticalAlignment.Center
        }

        Dim boton As New Button

        Dim grid As New Grid

        Dim imagenFondo As New ImageEx With {
            .Source = juego.ImagenGrande,
            .IsCacheEnabled = True,
            .Stretch = Stretch.UniformToFill,
            .Padding = New Thickness(0, 0, 0, 0),
            .HorizontalAlignment = HorizontalAlignment.Center,
            .VerticalAlignment = VerticalAlignment.Center
        }

        grid.Children.Add(imagenFondo)

        If Not juego.ImagenLogo = String.Empty Then
            Dim imagenLogo As New ImageEx With {
                .Source = juego.ImagenLogo,
                .IsCacheEnabled = True,
                .Stretch = Stretch.Uniform,
                .Padding = New Thickness(0, 0, 0, 0),
                .Margin = New Thickness(20, 20, 20, 20),
                .HorizontalAlignment = HorizontalAlignment.Center,
                .VerticalAlignment = VerticalAlignment.Bottom
            }

            grid.Children.Add(imagenLogo)
        End If

        boton.Tag = juego
        boton.Content = grid
        boton.Padding = New Thickness(0, 0, 0, 0)
        boton.Background = New SolidColorBrush(Colors.Transparent)

        panel.Content = boton

        Dim tbToolTip As TextBlock = New TextBlock With {
            .Text = juego.Titulo,
            .FontSize = 16,
            .TextWrapping = TextWrapping.Wrap
        }

        ToolTipService.SetToolTip(boton, tbToolTip)
        ToolTipService.SetPlacement(boton, PlacementMode.Mouse)

        AddHandler boton.Click, AddressOf BotonTile_Click
        AddHandler boton.PointerEntered, AddressOf UsuarioEntraBoton
        AddHandler boton.PointerExited, AddressOf UsuarioSaleBoton

        gv.Items.Add(panel)

    End Sub

    Private Sub BotonTile_Click(sender As Object, e As RoutedEventArgs)

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim botonJuego As Button = e.OriginalSource
        Dim juego As Tile = botonJuego.Tag

        Dim botonAñadirTile As Button = pagina.FindName("botonAñadirTile")
        botonAñadirTile.Tag = juego

        Dim imagenJuegoSeleccionado As ImageEx = pagina.FindName("imagenJuegoSeleccionado")
        imagenJuegoSeleccionado.Source = New BitmapImage(New Uri(juego.ImagenMediana))

        Dim tbJuegoSeleccionado As TextBlock = pagina.FindName("tbJuegoSeleccionado")
        tbJuegoSeleccionado.Text = juego.Titulo

        Dim gridSeleccionarJuego As Grid = pagina.FindName("gridSeleccionarJuego")
        gridSeleccionarJuego.Visibility = Visibility.Collapsed

        Dim gvTiles As GridView = pagina.FindName("gvTiles")

        If gvTiles.ActualWidth > anchoColumna Then
            ApplicationData.Current.LocalSettings.Values("ancho_grid_tiles") = gvTiles.ActualWidth
        End If

        gvTiles.Width = anchoColumna
        gvTiles.Padding = New Thickness(0, 0, 15, 0)

        Dim gridAñadir As Grid = pagina.FindName("gridAñadirTile")
        gridAñadir.Visibility = Visibility.Visible

        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("tile", botonJuego)

        Dim animacion As ConnectedAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("tile")

        If Not animacion Is Nothing Then
            animacion.TryStart(gridAñadir)
        End If

        Dim tbTitulo As TextBlock = pagina.FindName("tbTitulo")
        tbTitulo.Text = Package.Current.DisplayName + " (" + Package.Current.Id.Version.Major.ToString + "." + Package.Current.Id.Version.Minor.ToString + "." + Package.Current.Id.Version.Build.ToString + "." + Package.Current.Id.Version.Revision.ToString + ") - " + juego.Titulo

        '---------------------------------------------

        Dim imagenPequeña As ImageEx = pagina.FindName("imagenTilePequeña")
        imagenPequeña.Source = Nothing

        Dim imagenMediana As ImageEx = pagina.FindName("imagenTileMediana")
        imagenMediana.Source = Nothing

        Dim imagenAncha As ImageEx = pagina.FindName("imagenTileAncha")
        imagenAncha.Source = Nothing

        If Not juego.ImagenPequeña Is Nothing Then
            imagenPequeña.Source = juego.ImagenPequeña
            imagenPequeña.Tag = juego.ImagenPequeña
        End If

        If Not juego.ImagenMediana Is Nothing Then
            imagenMediana.Source = juego.ImagenMediana
            imagenMediana.Tag = juego.ImagenMediana
        End If

        If Not juego.ImagenAncha = Nothing Then
            imagenAncha.Source = juego.ImagenAncha
            imagenAncha.Tag = juego.ImagenAncha
        End If

        Dim imagenGrande As ImageEx = pagina.FindName("imagenTileGrande")

        If Not juego.ImagenGrande = Nothing Then
            imagenGrande.Source = juego.ImagenGrande
            imagenGrande.Tag = juego.ImagenGrande
        End If

    End Sub

    Private Sub UsuarioEntraBoton(sender As Object, e As PointerRoutedEventArgs)

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim gvTiles As AdaptiveGridView = pagina.FindName("gvTiles")

        Dim boton As Button = sender

        boton.Saturation(0).Scale(1.05, 1.05, gvTiles.DesiredWidth / 2, gvTiles.ItemHeight / 2).Start()

        Window.Current.CoreWindow.PointerCursor = New CoreCursor(CoreCursorType.Hand, 1)

    End Sub

    Private Sub UsuarioSaleBoton(sender As Object, e As PointerRoutedEventArgs)

        Dim frame As Frame = Window.Current.Content
        Dim pagina As Page = frame.Content

        Dim gvTiles As AdaptiveGridView = pagina.FindName("gvTiles")

        Dim boton As Button = sender

        boton.Saturation(1).Scale(1, 1, gvTiles.DesiredWidth / 2, gvTiles.ItemHeight / 2).Start()

        Window.Current.CoreWindow.PointerCursor = New CoreCursor(CoreCursorType.Arrow, 1)

    End Sub

End Module
