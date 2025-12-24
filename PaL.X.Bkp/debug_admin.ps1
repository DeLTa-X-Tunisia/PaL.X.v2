try {
    & "c:\Users\azizi\OneDrive\Desktop\PaL.X\src\PaL.X.Admin\bin\Debug\net9.0-windows\PaL.X.Admin.exe" 2>&1 | Out-File "c:\Users\azizi\OneDrive\Desktop\PaL.X\admin_error.log"
} catch {
    $_ | Out-File "c:\Users\azizi\OneDrive\Desktop\PaL.X\admin_error.log" -Append
}
