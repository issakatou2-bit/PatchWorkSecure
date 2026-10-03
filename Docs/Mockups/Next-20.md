# 次の実装 20 — 10大脅威の子たちのデータ（画面はまだ作らない）（Claude、2026-10-03）

**Next-19は完了済み。すぐ始めてよい。** 強敵を「IPA 情報セキュリティ10大脅威 2026（組織編）の擬人化」にする準備として、**データとテストだけ**を入れる。画面・絵・声・数値（抽選・採点・脅威の強さ）は変えない。今の強敵（`OpsYearContent`の7体）の動きも変えない。
設計：`Docs/Rivals-Persona-Design-2026-10-03.md`、キャラ：`Docs/Rivals-Characters-2026-10-03.md`、調べ：`Docs/Rivals-Threat-Research-2026-10-03.md`（00章がIPAの解説書で確かめた用語）。

## ① 脅威のカタログ（`Assets/Scripts/CompanyOps/OpsThreatCatalog.cs`、Unityに依存しない）

- `OpsAttackVector`（攻撃の糸口。解説書の表1.2の6つ）：`Vulnerability`（ソフトウェアの脆弱性）、`Malware`（マルウェアの利用）、`PasswordTheft`（パスワード窃取）、`Misconfiguration`（設定不備）、`DataEncryption`（データの暗号化）、`SocialEngineering`（ソーシャルエンジニアリング）。それぞれに「対策の基本」の文（解説書の表1.2の文のまま）。
- `OpsThreat`：`rank`、`ipaName`（解説書の脅威の名前そのまま）、`charName`（**仮**）、`nickname`（**仮**）、`color`（下の表）、`pupil`（瞳の形の説明）、`techniques`、`weaknesses`（効く設備のid）、`rivalIds`（今の強敵とのつながり）、`source`（`https://www.ipa.go.jp/security/10threats/10threats2026.html`）、`checked`（`2026-10-03`）。
- `OpsTechnique`：`name`（解説書の用語）、`vector`（糸口）、`summary`（1文。解説書の意味のまま）。
- 名前・呼び名はすべて**仮**（加藤さんが重なりの確認の後に決める）。`charName`が仮であることをコメントに書く。

| 順位 | ipaName | charName（仮） | color | 技（name → vector） | weaknesses（設備のid） | rivalIds |
|---|---|---|---|---|---|---|
| 1 | ランサム攻撃による被害 | クリプタ | 赤 `#D7263D` | ランサムウェア→DataEncryption／二重脅迫→DataEncryption／ノーウェアランサム→SocialEngineering／VPN機器の脆弱性からの侵入→Vulnerability | backup, edr, segment, patch, drill | y2-ransom, y3-final |
| 2 | サプライチェーンや委託先を狙った攻撃 | サプラ | 青緑 `#14A098` | 委託先を経由した侵入→PasswordTheft／ソフトウェアサプライチェーン攻撃→Malware／MSPの管理ソフトの悪用→Misconfiguration | zeroTrust, inventory, mfa, patch | y2-supply |
| 3 | AIの利用をめぐるサイバーリスク | ハルシ | 紫 `#8E44AD` | シャドーAI→Misconfiguration／ハルシネーション→SocialEngineering／間接プロンプトインジェクション→SocialEngineering／ディープフェイクによるなりすまし→SocialEngineering | education, runbook | y2-ai, y3-ai |
| 4 | システムの脆弱性を悪用した攻撃 | ヴァルネラ | 黄緑 `#8BC34A` | ゼロデイ攻撃→Vulnerability／Nデイ攻撃→Vulnerability／攻撃ツール・攻撃サービスの悪用→Vulnerability | patch, inventory, monitor, segment | （なし） |
| 5 | 機密情報を狙った標的型攻撃 | スピア | 水色 `#29B6F6` | 標的型攻撃メール→SocialEngineering／水飲み場型攻撃→Malware／ネットワーク貫通型攻撃→Vulnerability | education, edr, monitor, segment, threatSharing | y3-targeted |
| 6 | 地政学的リスクに起因するサイバー攻撃（情報戦を含む） | コグニ | 朱 `#F4511E` | 偽情報の流布→SocialEngineering／ランサム攻撃を偽装した攻撃→DataEncryption／スピアフィッシングによる情報窃取→SocialEngineering | education, threatSharing, backup, csirt | （なし） |
| 7 | 内部不正による情報漏えい等 | プリヴィ | 赤紫 `#C2185B` | アクセス権限の悪用→Misconfiguration／在職中に割り当てられたアカウントの悪用→PasswordTheft／内部情報の不正な持ち出し→Misconfiguration | inventory, monitor, runbook, education | （なし） |
| 8 | リモートワーク等の環境や仕組みを狙った攻撃 | ヴィピ | 黒と白銀 `#455A64` | リモートワーク用製品の脆弱性の悪用→Vulnerability／アカウント情報の不正利用→PasswordTheft／リモートワーク用端末への攻撃→Malware | patch, mfa, zeroTrust, edr | （なし） |
| 9 | DDoS攻撃（分散型サービス妨害攻撃） | フラッダ | 青 `#1E63D6` | ボットネットを利用したDDoS攻撃→Malware／フラッド攻撃→Misconfiguration／リフレクション攻撃→Misconfiguration／ランダムサブドメイン攻撃（DNS水責め攻撃）→Misconfiguration | redundancy, monitor, runbook | （なし） |
| 10 | ビジネスメール詐欺 | スプーフィ | 琥珀 `#F5A623` | 取引先へのなりすまし→SocialEngineering／経営者等へのなりすまし→SocialEngineering／BECの準備としての情報窃取→PasswordTheft | education, runbook, mfa | y3-bec |

- 技の`summary`は、`Docs/Rivals-Threat-Research-2026-10-03.md`の00章（解説書の◆の見出しと本文）の意味で1文にする。迷うものは書かずに空にし、報告で「要相談」として挙げる（推測で書かない）。
- DDoSのフラッド攻撃・リフレクション攻撃の糸口は、解説書の6分類に完全には収まらない。今は「設定不備（防御の設定・準備の不足）」に置き、カタログのコメントに「近い分類として置いた」と書く。
- 今の強敵のつながり（`rivalIds`）は表示に使わない。今の強敵の名前・説明・強さはそのまま。

## ② テスト（`Assets/Tests/`、普段の組）

- 10件そろい、順位1〜10が重ならず、`ipaName`が上の表の文字と完全一致すること。
- どの脅威も技が3つ以上、すべての技に糸口があること。
- `weaknesses`のidが、今の設備のカタログにすべて存在すること（存在しないidがあれば失敗）。
- 6つの糸口すべてに、効く設備が1つ以上あること（例：Vulnerability→patch、Malware→edr、PasswordTheft→mfa、Misconfiguration→inventory、DataEncryption→backup、SocialEngineering→education）。
- `rivalIds`がすべて今の強敵のidに存在すること。
- 色が10件ですべて違うこと。
- 1年だけの遊び・3年本編・終わりなき年度の数字が変わらないこと（Verify系と1年だけの900年度・10,779月のCSVのSHA256）。

## ③ 確認

- 関係するテストとコンパイル確認をコミット前に。最後は普段の組＋Captureを1回（Next-19の決まり）。Windows版の作り直しは不要（画面は変わらない）。
- `CLAUDE.md`と`AGENTS.md`の6・7を更新（両方を同じ内容に）。
- 終わったら新しい機能や見た目の変更は始めない。`summary`を空にしたもの（要相談）の一覧を報告に入れる。
