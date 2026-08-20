using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DedektiflikRPG.Core.Interfaces;
using DedektiflikRPG.Models;

namespace DedektiflikRPG.Services.Forensic;

public class ForensicService : IForensicService
{
    private readonly List<string> _submittedFindings = new();

    public void ClearFindings()
    {
        lock (_submittedFindings)
        {
            _submittedFindings.Clear();
        }
    }

    public void SubmitFinding(int clueId, string clueName, string findingText, List<NPC> npcs, int guiltyId)
    {
        if (string.IsNullOrWhiteSpace(findingText)) return;

        bool isGolge = (clueId >= 1000);
        int ownerNpcId = isGolge ? (clueId / 10) : Math.Clamp((clueId - 1) / 3 + 1, 1, 5);
        bool isGuiltyClue = (ownerNpcId == guiltyId);
        bool isFingerprint = findingText.Contains("PARMAK İZİ", StringComparison.OrdinalIgnoreCase);
        bool isBlood = findingText.Contains("KAN LEKESİ", StringComparison.OrdinalIgnoreCase);

        string sampleCode = isGolge ? $"NUMUNE #{clueId} ({clueName})" : (clueId switch
        {
            1 => "NUMUNE #01 (Masif Çelik Kesici Alet)",
            2 => "NUMUNE #02 (Veresiye Belgesi / Defter)",
            3 => "NUMUNE #03 (Tekstil Dokuma Önlük)",
            4 => "NUMUNE #04 (Cam Solüsyon Şişesi)",
            5 => "NUMUNE #05 (Reçete Notu)",
            6 => "NUMUNE #06 (Organik Bitkisel Örnek)",
            7 => "NUMUNE #07 (Yazılı Mektup Belgesi)",
            8 => "NUMUNE #08 (Optik Polimer Çerçeve)",
            9 => "NUMUNE #09 (Metal Kasa Bölmesi)",
            10 => "NUMUNE #10 (Metal Simgesel Rozet)",
            11 => "NUMUNE #11 (Resmi Tahkikat Belgesi)",
            12 => "NUMUNE #12 (Palto Düğmesi)",
            13 => "NUMUNE #13 (Sentetik İplik Lif Bobini)",
            14 => "NUMUNE #14 (Yün Kumaş Parçası)",
            15 => "NUMUNE #15 (Astar İçi Dikiş Bölmesi)",
            _ => $"NUMUNE #{clueId:D2} (Olay Yeri Materyali)"
        });

        string entry;

        if (isFingerprint)
        {
            if (isGuiltyClue)
            {
                entry = $"[🔬 DAKTİLOGRAFİK İNCELEME - {sampleCode}]: Nesne yüzeyindeki gizli parmak izi, EMNİYET KAYITLARINDAKİ ŞÜPHELİ PROFİLİ İLE %99.8 EŞLEŞTİ. (Cinayet mahallindeki doğrudan şüpheli teması doğrulanmıştır).";
            }
            else
            {
                entry = $"[🔬 DAKTİLOGRAFİK İNCELEME - {sampleCode}]: Nesne yüzeyindeki izler incelendi. Parmak izi rutin günlük kullanıcı teması ile uyumludur. (Cinayet anına ait şüpheli temas saptanmamıştır).";
            }
        }
        else if (isBlood)
        {
            if (isGuiltyClue)
            {
                string victimName = isGolge ? "Ekrem Bey" : "Osman Bey";
                entry = $"[🧬 SEROLOJİK DNA ANALİZİ - {sampleCode}]: Numunedeki kan lekesi ve DNA serotipi, kurban {victimName}'in kan profili ile TAM EŞLEŞTİ. (Cinayet anı arbede/temas lekesi kesinleşmiştir).";
            }
            else
            {
                entry = $"[🧬 SEROLOJİK DNA ANALİZİ - {sampleCode}]: Serolojik test tamamlandı. Lekedeki biyolojik yapı kurbana ait DEĞİLDİR (Büyükbaş/hayvan kanı, boya veya rutin iş materyalidir - Cinayet ile ilişkisi saptanmamıştır).";
            }
        }
        else
        {
            entry = $"[🔬 ADLİ LAB İNCELEMESİ - {sampleCode}]: {findingText}";
        }

        lock (_submittedFindings)
        {
            if (!_submittedFindings.Contains(entry))
            {
                _submittedFindings.Add(entry);
            }
        }
    }

    public Task<string> GenerateAutopsyReportAsync(List<NPC> npcs, int guiltyId)
    {
        var guiltyNpc = npcs.FirstOrDefault(n => n.NPCId == guiltyId);
        string guiltyName = guiltyNpc?.Name ?? "Bilinmiyor";
        bool isGolge = (guiltyId >= 100);

        string victimName = isGolge ? "Ekrem Bey (62, Erkek)" : "Osman Bey (58, Erkek)";
        string deathTime = isGolge ? "02:00 - 02:30 (Çam Ormanı Yolu / Göl Kenarı)" : "23:45 - 00:30 (Mantarlaşma & Rigor Mortis)";

        string reportHtml = $@"
        <div class='autopsy-dossier'>
            <div class='autopsy-paper-header'>
                <div class='autopsy-official-seal'>
                    <i class='fa-solid fa-scale-balanced'></i> T.C. ADLİ TIP KURUMU OTOPSİ VE ADLİ BİLİMLER BAŞKANLIĞI
                </div>
                <div class='autopsy-dossier-no'>RESMİ OTOPSİ PROTOKOL DOSYASI #{(isGolge ? "208-G" : "104-B")}</div>
            </div>
            
            <div class='autopsy-meta-grid'>
                <div class='autopsy-meta-item'><strong>KURBAN:</strong> {victimName}</div>
                <div class='autopsy-meta-item'><strong>TAHMİNİ ÖLÜM SAATİ:</strong> {deathTime}</div>
                <div class='autopsy-meta-item'><strong>OTOPSİ UZMANI:</strong> Dr. Selim Karaca (Adli Tabip)</div>
                <div class='autopsy-meta-item'><strong>GİZLİLİK DERECESİ:</strong> <span class='autopsy-alert-tag'>🚨 ÇOK GİZLİ / İL MÜHÜRLÜ</span></div>
            </div>

            <div class='autopsy-divider-line'></div>

            <div class='autopsy-sec-title'><i class='fa-solid fa-microscope'></i> OTOPSİ & ADLİ PATOLOJİ BULGULARI</div>
            <div class='autopsy-main-finding'>";

        if (isGolge)
        {
            switch (guiltyId)
            {
                case 101: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[PATOLOJİK KESİ]</span> Kafatasının sol frontal bölgesinde ağır masif kesici aletle açılmış 16cm derin yarıklı darbe lezyonu saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[MEKANİK ANATOMİ]</span> Darbenin yüksek kuvvetle ve dikey açıyla uygulandığı, kemik dokuda derin çatlaklar bıraktığı saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[EPİDERMAL MİKROSKOBİ]</span> Kurbanın ense kısmında mikronize bitkisel reçine ve odunsu kalıntılar tespit edilmiştir.</div>";
                    break;
                case 102: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[PENETRASYON TRAVMASI]</span> Göğüs anterior mediastinal bölgesinde keskin tek kenarlı alet girişiyle (vulnus punctum) oluşan ölümcül yara saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[İÇ PERFÜZYON]</span> Kesi çevresinde organik asit kalıntıları ile uyumlu eser miktarda asidik biyokimyasal izole edilmiştir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[DOKU PATOLOJİSİ]</span> Kurbanın ceket liflerinde koyu renkli sentetik olmayan mikro-iplikler saptanmıştır.</div>";
                    break;
                case 103: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[TRAVMA ANALİZİ]</span> Oksipital kemikte sıcak/kızgın silindirik sert cisim darbesi ile uyumlu dairesel yanık lezyonlu kırık saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[METALOGRAFİK ANALİZ]</span> Darbe soketinde mikroskobik ölçekte inorganik metalik toz ve kömür kalıntıları saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[PİGMENT KANITI]</span> Saç diplerinde yüksek ısı kaynağına maruziyetten kaynaklanan is ve endüstriyel yağ lekesi izole edilmiştir.</div>";
                    break;
                case 104: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[İÇ TOKSİKOLOJİ]</span> Akut pulmoner ödem ve solunum depresyonu bulguları sabittir. Mekanik darbe izine rastlanmamıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[TOKSİKOLOJİK PATOLOJİ]</span> Kurbanın gastrik sıvısında ve bronş çeperlerinde yüksek konsantrasyonda arsenik bazlı inorganik toksin tortuları izole edilmiştir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[KİMYASAL BULGU]</span> Maktulün ağız içi mukozasında zehir karıştırılmış kurutulmuş bitki yaprağı lifleri tespit edilmiştir.</div>";
                    break;
                case 105: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[EKSTERN BİLGİ]</span> Miyokardiyal aritmi ve kardiyovasküler felç bulguları saptanmıştır. Herhangi bir fiziki darbe veya penetrasyon saptanmamıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[TOKSİKOLOJİK ROL]</span> Kan ve idrar tahlillerinde atropin ve skopolamin alkaloid bileşenleri izole edilmiştir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[POZOLOJİK UYGULAMA]</span> Zehrin kurbanın günlük olarak tükettiği farmakolojik bir karışıma enjekte edildiği anlaşılmıştır.</div>";
                    break;
                case 106: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[TRAVMA DETAYI]</span> Kafatasının temporal mevkisinde ağır pürüzsüz metal cisim darbesiyle oluşan çökme kırığı saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[EPİDERMAL MİKROSKOBİ]</span> Kurbanın tırnak aralarında inorganik kırmızı balmumu (Rubrum Cera) pigmentleri saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[KANTİTATİF ANALİZ]</span> Bel bölgesinde dekoratif halı lifleri ve optik polimer cam kıymıkları izole edilmiştir.</div>";
                    break;
                case 107: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[POSTÜRAL TRAVMA]</span> Oksipital kemikte sert bir yapıya çarpmasıyla uyumlu doğrusal kırılma (fractura linearis) ve beyin kanaması saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[MEKANİK ANALİZ]</span> Kurbanın elbisesinde saat 02:14 sularında durmuş olan mekanik bir aksama ait pirinç zincir halkası saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[DOKU ANALİZİ]</span> Olay yerinde ve kurban elbiselerinde yüksek nem oranına sahip toprak yapısı ve petrokimyasal aydınlatma yakıtı kalıntıları izole edilmiştir.</div>";
                    break;
                case 108: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[ASFİKSİ ANOMALİSİ]</span> Boyun trakeal kıkırdak çevresinde 0.4mm kalınlığında dayanıklı sentetik sicimle (ligatür boğulma) oluşan derin strangülasyon çizgisi saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[MİKROSKOBİK TESPİT]</span> Strangülasyon çizgisinde izolasyon materyali (mumlu kaplama) ve endüstriyel yapıştırıcı kalıntıları izole edilmiştir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[İZ ANALİZİ]</span> Kurbanın ceket omuzunda 42 ebatlarında organik dokulu ağır ayakkabı tabanı izi saptanmıştır.</div>";
                    break;
                default:
                    reportHtml += "<div class='autopsy-bullet'>• Otopsi ve laboratuvar analizleri devam ediyor.</div>";
                    break;
            }
        }
        else
        {
            switch (guiltyId)
            {
                case 1: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[PATOLOJİK KESİ]</span> Boyun sol karotis arter mevkisinde enlemesine, geniş ağızlı masif kesici aletle (incisio vulnificus) açılmış 14cm derin yırtıklı yara saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[MEKANİK ANATOMİ]</span> Darbenin yüksek kas aktivitesi ve kesici/bileyici alet kullanım tecrübesi olan bir şahıs tarafından tek hamlede uygulandığı değerlendirilmiştir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[EPİDERMAL MİKROSKOBİ]</span> Kurbanın tırnak altında yapılan spektrofotometrik taramada inorganik bileme tozu izole edilmiştir.</div>";
                    break;
                case 2: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[EKSTERN İNCELEME]</span> Post-mortem muayenede gövde üzerinde mekanik arbede veya fiziki darbe izine rastlanmamıştır. Miyokardial paralizi ve siyanoz bulguları sabittir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[TOKSİKOLOJİK ROL]</span> Kan ve gastrik sıvı örneklerinde nörotoksik kardiyovasküler felce neden olan fitotoksin glikozit bileşeni tespit edilmiştir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[POZOLOJİK UYGULAMA]</span> Toksik ajanın kurbanın düzenli tükettiği farmakolojik bir solüsyona enjekte edildiği anlaşılmıştır.</div>";
                    break;
                case 3: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[TRAVMA ANALİZİ]</span> Kafatasının sağ parietal osteo-sutur bölgesinde ağır metalli nesne soketi ile uyumlu oval çökme kırığı (fractura depressa) saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[DOKU SPEKTROSKOBİSİ]</span> Göğüs lezyonlarında selülozik kağıt lifleri, kırık optik polimer mikro-kıymıkları ve kalsiyum karbonat tespit edilmiştir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[PİGMENT KANITI]</span> Elbise astarları ve tırnak aralarında kırmızı pigmentli baskı materyali (Rubrum Impressio) kalıntıları izole edilmiştir.</div>";
                    break;
                case 4: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[DOĞRUSAL TRAVMA]</span> Sırt ve thorakal kaburga mevkisinde 32mm silindirik esnek yapılı bir aksesuara ait doğrusal ezik izleri (contusio rectilinearis) saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[ASFİKSİ BULGUSU]</span> Boyun hyoid kemik çevresinde aksiller kıskac altında subkonjonktival asphyxia hematomu mevcuttur.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[KİMYASAL ANOMALİ]</span> Olay yerinde ve kurban elbisesinde adli incelemeyi engellemeye yönelik profesyonel endüstriyel solvent solüsyonu kalıntıları tespit edilmiştir.</div>";
                    break;
                case 5: 
                    reportHtml += @"
                    <div class='autopsy-bullet'><span class='bullet-tag'>[LİGATÜR BOĞULMA]</span> Boyun anterior larynx bölgesinde 0.4mm çapında çok ince, yüksek tensil mukavemetli sentetik lif (sulcus strangulationis) saptanmıştır.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[LİF PATOLOJİSİ]</span> Kurbanın posterior ceket astarlarında keskin lezyon ve organik dokulu mikronize yün lifleri izole edilmiştir.</div>
                    <div class='autopsy-bullet'><span class='bullet-tag'>[POSTÜRAL ANALİZ]</span> Saldırının kurbanın beklemediği bir anda arka açıdan dairesel traksiyon hamlesiyle gerçekleştirildiği kesinleşmiştir.</div>";
                    break;
                default:
                    reportHtml += "<div class='autopsy-bullet'>• Otopsi ve laboratuvar analizleri devam ediyor.</div>";
                    break;
            }
        }

        reportHtml += "</div>";

        List<string> currentFindings;
        lock (_submittedFindings)
        {
            currentFindings = _submittedFindings.ToList();
        }

        if (currentFindings.Count > 0)
        {
            reportHtml += @"
            <div class='autopsy-divider-line'></div>
            <div class='autopsy-sec-title'><i class='fa-solid fa-vial-circle-check'></i> DEDEKTİF ADLİ LAB EŞLEŞTİRME RAPORLARI</div>
            <div class='autopsy-findings-list'>";

            foreach (var finding in currentFindings)
            {
                bool isMatch = finding.Contains("EŞLEŞTİ!");
                string badgeClass = isMatch ? "badge-match" : "badge-nomatch";
                string badgeIcon = isMatch ? "<i class='fa-solid fa-circle-check'></i>" : "<i class='fa-solid fa-circle-exclamation'></i>";

                reportHtml += $@"
                <div class='autopsy-finding-card {badgeClass}'>
                    <div class='finding-header'>{badgeIcon} {finding}</div>
                </div>";
            }

            reportHtml += "</div>";
        }
        else
        {
            reportHtml += @"
            <div class='autopsy-divider-line'></div>
            <div class='autopsy-sec-title'><i class='fa-solid fa-microscope'></i> LABORATUVAR İNCELEME DURUMU</div>
            <p style='color: #888; font-style: italic; font-size: 0.95rem;'>Henüz olay yerinden adli tıbba nesne incelemesi gönderilmedi. Binalardaki nesneleri büyüteç, UV veya toz fırçasıyla inceleyip 'ADLİ TIBBA GÖNDER' butonunu kullanabilirsiniz.</p>";
        }

        reportHtml += "</div>";

        return Task.FromResult(reportHtml);
    }

    public object GetForensicState(int guiltyId)
    {
        int weaponId = 0;
        int fingerprintId = 0;

        if (guiltyId >= 100)
        {
            switch (guiltyId)
            {
                case 101: weaponId = 1011; fingerprintId = 1013; break; // Oduncu
                case 102: weaponId = 1023; fingerprintId = 1021; break; // Manav
                case 103: weaponId = 1031; fingerprintId = 1033; break; // Demirci
                case 104: weaponId = 1043; fingerprintId = 1042; break; // Bakkal
                case 105: weaponId = 1051; fingerprintId = 1054; break; // Hekim
                case 106: weaponId = 1064; fingerprintId = 1062; break; // Muhtar
                case 107: weaponId = 1073; fingerprintId = 1071; break; // Fehmi
                case 108: weaponId = 1084; fingerprintId = 1081; break; // Kunduracı
            }
        }
        else
        {
            switch (guiltyId)
            {
                case 1: weaponId = 1; fingerprintId = 2; break; // Kasap
                case 2: weaponId = 6; fingerprintId = 4; break; // Eczacı
                case 3: weaponId = 8; fingerprintId = 7; break; // Muhtar
                case 4: weaponId = 10; fingerprintId = 12; break; // Komiser
                case 5: weaponId = 13; fingerprintId = 14; break; // Terzi
            }
        }

        return new { success = true, guiltyId, weaponId, fingerprintId };
    }

    public string GetDynamicClueDetail(int clueId, int guiltyId)
    {
        return clueId switch
        {
            1 => "Üzerindeki kan lekeleri Osman Bey'e ait gibi görünüyor. " + (guiltyId == 1 ? "Sapındaki el izi net bir şekilde Kasap Hasan'ı işaret ediyor." : "Ancak satırın sapında kasaba ait olmayan, eldivenle tutulmuş gibi garip izler var."),
            2 => "Sayfalarda Osman Bey'in adı kırmızıyla çizilmiş. Yanında bir not: " + (guiltyId == 1 ? "'Borcunu ödemedi, cezasını çekecek.'" : "'Bu borç sadece başlangıç.'"),
            3 => "Kavga izleri taşıyan önlük... Osman Bey'in ceketinin düğmesi önlüğün cebinde bulunuyor. " + (guiltyId == 1 ? "Hasan o gece kurbanla boğuşmuş olmalı." : "Önlük üzerindeki leke iş yeri lekesi gibi duruyor."),
            4 => "Zehirli bir ilacın boş şişesi. Reçetede Osman Bey'in adı var. " + (guiltyId == 2 ? "Etiketin arkasında Selma'nın el yazısıyla 'Son Doz' yazıyor." : "Şişe aceleyle alınmış gibi, kapağı zorlanmış."),
            5 => "Osman Bey'e verilen ilaçların listesi. Sayfa ortadan dikey yırtılmış. " + (guiltyId == 2 ? "Yırtık sayfanın kenarında 'Zehir' kelimesi okunabiliyor." : "Birisi kanıtları yok etmek için defteri zorla yırtmış."),
            6 => "Bu bitkinin özü, Osman Bey'in kanında bulunan zehirle aynı. " + (guiltyId == 2 ? "Selma bunu kasten hazırlamış." : "Birisi Selma'nın dükkanından bu otu gizlice almış olabilir."),
            7 => "Mektupta 'Osman, o araziler benim, sonun yaklaşıyor' yazıyor. " + (guiltyId == 3 ? "Muhtar Kemal açıkça kurbanı tehdit etmiş ve bunu gerçekleştirmiş." : "Ancak mektup asla postalanmamış, sadece bir sinir anında yazılmış."),
            8 => "Osman Bey'in kırık gözlüğü... " + (guiltyId == 3 ? "Muhtarın odasında şiddetli bir kavga yaşanmış." : "Gözlük bir başka yerde kırılıp buraya bırakılmış olabilir."),
            9 => "Kasada Osman Bey'in arazilerine ait sahte tapular var. " + (guiltyId == 3 ? "Kemal her şeyi planlamış, cinayet sebebi bu tapular." : "Bu tapular sadece muhtarın açgözlülüğünü gösteriyor, cinayeti değil."),
            10 => "Rozetin numarası kazınmış. Osman Bey'in cesedinin hemen yanında bulundu. " + (guiltyId == 4 ? "Güneş, kurbanla olay yerinde boğuşurken rozetini düşürmüş." : "Rozet oraya özellikle bir polisi suçlamak için bırakılmış."),
            11 => "Dosyada Osman Bey'in gizli geçmişi var. " + (guiltyId == 4 ? "Komiser Güneş, bu geçmişi kullanarak kurbanı şantaj yapıyordu." : "Dosya sadece prosedür gereği tutulmuş."),
            12 => "Pahalı bir palto düğmesi. Osman Bey'in cebinden çıktı. " + (guiltyId == 4 ? "Güneş'in paltosundan kopmuş, arbede sırasında Osman Bey onu tutmuş." : "Bu düğme terzinin bir müşterisine de ait olabilir."),
            13 => "İplik, Osman Bey'in ceketinin dikişleriyle aynı. Üzerindeki kan... " + (guiltyId == 5 ? "Kurbanın kanı. Yahya kurbanı öldürürken makara elindeydi." : "Terzinin dikiş yaparken kendi elini kestiği bir kaza olabilir."),
            14 => "Osman Bey'in ceketinden kopan kumaş. " + (guiltyId == 5 ? "Yahya kurbanla boğuşurken kumaş yırtıldı." : "Kumaş sadece bir terzi artığı olabilir."),
            15 => "Cepteki notta 'Osman, bugün hava kararınca gel konuşalım' yazıyor. " + (guiltyId == 5 ? "Yahya onu çağırdı ve tuzağa düşürdü." : "Yahya çağırdı ama gittiğinde onu ölü buldu."),
            _ => "Bu nesne karanlık sırlar barındırıyor..."
        };
    }
}
