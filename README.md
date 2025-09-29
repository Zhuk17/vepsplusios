# VEPS_Plus

**Для установки иконки добавить файл в Appicon, в файле проекта заменить строчку по аналогии. В AndroidManifest заменить имя файла иконки*

**Для установки экрана-заставки добавить файл в Splash. В свойствах файла: действие при сборке - MauiSplashScreen. Убрать из файла проекта строки Remove для файла, если они добавились:*
	<.ItemGroup>
	<.None Remove="Resources\Splash\splash_text.png" />
	<.ItemGroup>