# 🚀 MiniSocial - ASP.NET Core MVC Social Media Platform

Modern ve katmanlı mimari ilkeleriyle geliştirilmiş, kullanıcıların kimlik doğrulama yapabildiği, gönderi paylaşabildiği, profilleri yönetebildiği ve anlık etkileşimde bulunabildiği tam kapsamlı bir sosyal medya web uygulamasıdır.

---

## 🛠️ Kullanılan Teknolojiler ve Mimari

Bu proje, modern .NET ekosisteminin endüstri standartlarındaki araçlarıyla geliştirilmiştir:

* **Backend:** C#, .NET (ASP.NET Core MVC)
* **ORM & Database:** Entity Framework Core, SQL Server, Code-First yaklaşımı
* **Mimari Yaklaşım:** N-Tier / Katmanlı Mimari ve Repository Pattern prensipleri
* **Frontend:** HTML5, CSS3, Bootstrap, JavaScript
* **Ağ & Dağıtım (Deployment):** Yerel testler ve dış dünyaya tünelleme için Cloudflare Tunnels (`cloudflared`)

---

## 🌟 Temel Özellikler

* **Güvenli Kimlik Doğrulama (Authentication & Authorization):** ASP.NET Core Identity altyapısı ile güvenli kayıt olma, oturum açma (Login/Logout) ve yetkilendirme mekanizmaları.
* **Akış (Feed) Yönetimi:** Kullanıcıların ana sayfada paylaşılan gönderileri listeleyebilmesi.
* **Profil Yönetimi:** Kullanıcıların kendi profillerini kişiselleştirmesi ve gönderilerini görüntülemesi.
* **Etkileşimler:** Gönderi paylaşma, beğenme ve yorum yapma altyapısı.
* **Veritabanı Yönetimi:** Entity Framework Core Migrations ile otomatik veritabanı oluşturma ve güncelleme süreçleri.

---

## ⚙️ Kurulum ve Çalıştırma

Projeyi kendi yerel ortamınızda çalıştırmak için aşağıdaki adımları takip edebilirsiniz:

1. **Depoyu Klonlayın:**
   ```bash
   git clone [https://github.com/oakkus620/NewProject.git](https://github.com/oakkus620/NewProject.git)
