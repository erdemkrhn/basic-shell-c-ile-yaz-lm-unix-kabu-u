# &#x20;Basic Shell — C# ile Yazılmış inux Kabuğu

> Bash, Fish ve Zsh'den ilham alınarak C# ile geliştirilmiş eğitim amaçlı bir Unix kabuğu.

\---

## &#x20;İçindekiler

* [Özellikler](#özellikler)
* [Kullanılan Sistem Çağrıları](#kullanılan-sistem-çağrıları)
* [Gereksinimler](#gereksinimler)
* [Derleme ve Çalıştırma](#derleme-ve-çalıştırma)
* [Kullanım Örnekleri](#kullanım-örnekleri)
* [Proje Yapısı](#proje-yapısı)
* [Mimari](#mimari)
* [Sınırlamalar](#sınırlamalar)
* [Katkıda Bulunma](#katkıda-bulunma)

\---

## &#x20;Özellikler

|Özellik|Açıklama|Örnek|
|-|-|-|
|**Harici komutlar**|PATH'teki herhangi bir program çalıştırılabilir|`ls -la`, `cat file.txt`, `date`|
|**Built-in: `cd`**|Dizin değiştirme|`cd /tmp`, `cd \~`, `cd ..`|
|**Built-in: `pwd`**|Mevcut dizini gösterme|`pwd`|
|**Built-in: `exit`**|Kabuktan çıkma|`exit`, `exit 1`|
|**Pipe (`\|`)**|Çok aşamalı komut zincirleri|`ls \| grep .txt \| wc -l`|
|**Çıkış yönlendirme (`>`)**|Çıkışı dosyaya yaz|`echo merhaba > dosya.txt`|
|**Ekleme modu (`>>`)**|Çıkışı dosyaya ekle|`date >> log.txt`|
|**Giriş yönlendirme (`<`)**|Dosyadan oku|`wc -l < dosya.txt`|
|**Renkli prompt**|`kullanıcı@makine:dizin$` biçiminde|Otomatik|
|**Tırnak desteği**|Boşluklu argümanlar için|`echo "merhaba dünya"`|
|**Yorum satırı**|`#` sonrası yok sayılır|`ls # dizini listele`|

\---

## &#x20;Kullanılan Sistem Çağrıları

MyShell, Linux çekirdeğinin temel sistem çağrılarını .NET/C# soyutlamaları aracılığıyla kullanır:

|Linux Sistem Çağrısı|C# Karşılığı|Kullanım Amacı|
|-|-|-|
|`fork()`|`new Process()`|Yeni alt süreç (child process) oluşturma|
|`exec()` / `execvp()`|`Process.Start()`|Alt sürece program yükleme ve çalıştırma|
|`waitpid()`|`Process.WaitForExit()`|Alt sürecin tamamlanmasını bekleme|
|`pipe()`|`Process.StandardOutput` / `StandardInput` akışları|Süreçler arası iletişim kanalı|
|`dup2()`|`ProcessStartInfo.RedirectStandard\*`|Dosya tanımlayıcılarını (FD) yeniden yönlendirme|
|`open()`|`new FileStream(...)`|Dosya açma (okuma/yazma/ekleme)|
|`close()`|`Stream.Dispose()` / `using` bloğu|Dosya tanımlayıcısını kapatma|
|`chdir()`|`Directory.SetCurrentDirectory()`|Çalışma dizinini değiştirme|
|`getcwd()`|`Directory.GetCurrentDirectory()`|Mevcut çalışma dizinini alma|
|`exit()`|`Environment.Exit()`|Süreci belirtilen çıkış koduyla sonlandırma|

\---

## &#x20;Gereksinimler

* **İşletim Sistemi:** Linux veya macOS (Windows'ta kısmi destek)
* **.NET SDK:** 6.0 veya üzeri

```bash
# .NET sürümünü kontrol et
dotnet --version
```

> \*\*Not:\*\* Windows üzerinde `ls`, `grep` gibi Unix komutları doğrudan çalışmaz.  
> WSL (Windows Subsystem for Linux) kullanılması önerilir.

\---

## &#x20;Derleme ve Çalıştırma

### Yöntem 1: `dotnet run` (Önerilen)

```bash
# Projeyi oluştur
mkdir MyShell \&\& cd MyShell
dotnet new console -n MyShell
cp MyShell.cs Program.cs   # veya dosyayı düzenle

# Çalıştır
dotnet run
```

### Yöntem 2: Manuel derleme

```bash
# .csproj ile derleme
dotnet build -o ./build
./build/MyShell

# Mono ile derleme (eski sistemler için)
mcs MyShell.cs -out:MyShell.exe
mono MyShell.exe
```

### Yöntem 3: Tek dosya yayını

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
./bin/Release/net8.0/linux-x64/publish/MyShell
```

\---

## &#x20;Kullanım Örnekleri

### Temel Komutlar

```bash
myshell$ whoami
ali

myshell$ date
Salı Tem  1 14:32:10 TRT 2025

myshell$ df -h
Filesystem      Size  Used Avail Use% Mounted on
/dev/sda1        50G   12G   36G  25% /

myshell$ ls -la /tmp
total 48
drwxrwxrwt 12 root root 4096 Tem  1 14:00 .
...
```

### Built-in Komutlar

```bash
myshell$ pwd
/home/ali

myshell$ cd /var/log
ali@makine:/var/log$ pwd
/var/log

myshell$ cd \~
ali@makine:\~$ pwd
/home/ali

myshell$ exit 0
Kabuktan çıkılıyor... Görüşürüz!
```

### I/O Yönlendirme

```bash
# Çıkışı dosyaya yaz (oluştur / üzerine yaz)
myshell$ echo "Merhaba Dünya" > selam.txt
myshell$ cat selam.txt
Merhaba Dünya

# Çıkışı dosyaya ekle (üzerine yazmaz)
myshell$ date >> log.txt
myshell$ date >> log.txt
myshell$ cat log.txt
Salı Tem  1 14:32:10 TRT 2025
Salı Tem  1 14:32:15 TRT 2025

# Dosyadan giriş oku
myshell$ wc -l < log.txt
2

# Çoklu yönlendirme
myshell$ sort < isimler.txt > sirali.txt
```

### Pipe Zincirleri

```bash
# Tek pipe
myshell$ ls /etc | grep "conf"
adduser.conf
apt.conf
...

# Çok aşamalı pipe
myshell$ cat /etc/passwd | grep "/bin/bash" | wc -l
3

# Pipe + yönlendirme
myshell$ ps aux | grep python | awk '{print $2}' > python\_pids.txt

# Metin işleme
myshell$ echo "merhaba dünya" | tr 'a-z' 'A-Z'
MERHABA DÜNYA
```

### Yardım

```bash
myshell$ help

── MyShell Komutları ──────────────────────────────
  Dahili Komutlar:
    cd \[dizin]    - Dizin değiştir
    pwd           - Mevcut dizini göster
    exit \[kod]    - Kabuktan çık
    help          - Bu yardım mesajını göster

  I/O Yönlendirme:
    komut > dosya    - Çıkışı dosyaya yaz (üzerine yaz)
    komut >> dosya   - Çıkışı dosyaya ekle
    komut < dosya    - Dosyadan giriş oku

  Pipe:
    komut1 | komut2  - komut1 çıkışını komut2'ye bağla
```

\---

## &#x20;Proje Yapısı

```
MyShell/
├── MyShell.cs          # Ana kaynak kodu (tek dosya implementasyon)
├── MyShell.csproj      # .NET proje dosyası
└── README.md           # Bu dosya
```

### Sınıf Yapısı

```
Program
└── Main()              ← Giriş noktası

Shell
├── Run()               ← REPL döngüsü
├── PrintPrompt()       ← Komut istemi gösterimi
│
├── ExecuteLine()       ← Komut satırı yöneticisi
├── SplitByPipe()       ← Pipe ayrıştırıcı
│
├── ExecuteSingleCommand()  ← Tek komut çalıştırıcı
├── ExecuteBuiltin()        ← Built-in komutlar
├── BuiltinCd()             ← cd implementasyonu
├── ExecuteExternal()       ← fork()+exec()+waitpid()
├── ExecutePipeline()       ← Pipe zinciri
│
├── ParseRedirects()    ← I/O yönlendirme ayrıştırıcı
├── TokenizeRaw()       ← Ham tokenizer (lexer)
└── TokenizeCommand()   ← Komut+argüman tokenizer
```

\---

## &#x20;Mimari

```
Kullanıcı Girişi
      │
      ▼
┌─────────────────┐
│   ReadLine()    │  ← read() sistem çağrısı
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  SplitByPipe()  │  ← Pipe var mı?
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
   Evet      Hayır
    │         │
    ▼         ▼
┌───────┐ ┌─────────────────────┐
│Pipeline│ │  ParseRedirects()   │
│Execute │ │  TokenizeCommand()  │
└───┬───┘ └──────────┬──────────┘
    │                 │
    │           ┌─────┴──────┐
    │         Built-in?    Harici?
    │           │              │
    │           ▼              ▼
    │     ExecuteBuiltin() ExecuteExternal()
    │           │              │
    │           │         fork()+exec()
    │           │         waitpid()
    │           │         dup2() (I/O)
    │           │              │
    └───────────┴──────────────┘
                │
                ▼
          Sonraki Prompt
```

\---

## &#x20;Sınırlamalar

Bu, eğitim amaçlı bir implementasyon olduğundan aşağıdaki özellikler **eklenmemiştir**:

|Özellik|Durum|Açıklama|
|-|-|-|
|Değişkenler (`$HOME`, `$PATH`)|❌|Ortam değişkeni genişletme yok|
|Glob/Wildcard (`\*.txt`)|❌|Joker karakter genişletme yok|
|Geçmiş (history)|❌|Önceki komutlara erişim yok|
|Sekme tamamlama|❌|Tab completion yok|
|Arka plan işler (`\&`)|❌|Job control yok|
|Sinyal yönetimi|❌|SIGINT, SIGTERM işleme yok|
|Here-doc (`<<`)|❌|Heredoc desteği yok|
|`cd -`|❌|Önceki dizine dönme yok|
|Betik dosyası|❌|`.sh` dosyası çalıştırma yok|

\---

## &#x20;Katkıda Bulunma

Projeyi geliştirmek için önerilen adımlar:

1. Repo'yu fork'layın
2. Özellik dalı oluşturun: `git checkout -b ozellik/glob-destegi`
3. Değişikliklerinizi commit'leyin: `git commit -m "Glob desteği eklendi"`
4. Pull request açın

### Geliştirme Fikirleri

* \[ ] `$DEĞIŞKEN` genişletme
* \[ ] `\*.txt` glob desteği
* \[ ] Komut geçmişi ve yukarı ok navigasyonu
* \[ ] Sekme tamamlama
* \[ ] `\&\&` ve `||` operatörleri
* \[ ] Arka plan çalıştırma (`komut \&`)
* \[ ] Betik dosyası desteği (`myshell script.sh`)

\---

## &#x20;Referanslar

* [The Linux Programming Interface - Michael Kerrisk](https://man7.org/tlpi/)
* [Advanced Programming in the UNIX Environment - Stevens \& Rago](https://www.apuebook.com/)
* [GNU Bash Referans Kılavuzu](https://www.gnu.org/software/bash/manual/)
* [`fork(2)` man sayfası](https://man7.org/linux/man-pages/man2/fork.2.html)
* [`execvp(3)` man sayfası](https://man7.org/linux/man-pages/man3/exec.3.html)
* [`pipe(2)` man sayfası](https://man7.org/linux/man-pages/man2/pipe.2.html)

\---

## &#x20;Lisans

MIT Lisansı — Eğitim ve kişisel kullanım için serbesttir.

\---

*Basic Shell — İşletim Sistemleri dersi için C# ile yazılmış örnek bir Unix kabuğu.*

