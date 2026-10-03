#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    // 組の印なし＝普段の組。画面・声・抽選・計算には接続しない資料データの点検。
    public sealed class CompanyOpsThreatCatalogTests
    {
        [Test] public void 十脅威の順位とIPA正式名が指定の表と完全一致する()
        {
            CollectionAssert.AreEqual(Enumerable.Range(1, 10), OpsThreatCatalog.Threats.Select(t => t.rank));
            CollectionAssert.AreEqual(new[] {
                "ランサム攻撃による被害", "サプライチェーンや委託先を狙った攻撃", "AIの利用をめぐるサイバーリスク",
                "システムの脆弱性を悪用した攻撃", "機密情報を狙った標的型攻撃",
                "地政学的リスクに起因するサイバー攻撃（情報戦を含む）", "内部不正による情報漏えい等",
                "リモートワーク等の環境や仕組みを狙った攻撃", "DDoS攻撃（分散型サービス妨害攻撃）", "ビジネスメール詐欺"
            }, OpsThreatCatalog.Threats.Select(t => t.ipaName));
        }

        [Test] public void 六つの糸口と対策の基本がIPA表一二の本文と一致する()
        {
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(OpsAttackVector)), OpsThreatCatalog.Vectors.Select(v => v.vector));
            CollectionAssert.AreEqual(new[] { "ソフトウェアの脆弱性", "マルウェアの利用", "パスワード窃取", "設定不備", "データの暗号化", "ソーシャルエンジニアリング（罠にはめる）" }, OpsThreatCatalog.Vectors.Select(v => v.name));
            CollectionAssert.AreEqual(new[] { "ソフトウェアの更新", "セキュリティソフトの利用", "パスワードの管理・認証の強化", "設定の見直し", "バックアップの取得", "脅威・手口を知る" }, OpsThreatCatalog.Vectors.Select(v => v.basic));
        }

        [Test] public void 各脅威に三つ以上の技と有効な糸口がある()
        {
            Assert.AreEqual(33, OpsThreatCatalog.Threats.Sum(t => t.techniques.Count));
            foreach (var threat in OpsThreatCatalog.Threats)
            {
                Assert.GreaterOrEqual(threat.techniques.Count, 3, threat.ipaName);
                Assert.AreEqual(threat.techniques.Count, threat.techniques.Select(t => t.name).Distinct().Count());
                foreach (var technique in threat.techniques)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(technique.name));
                    Assert.IsTrue(Enum.IsDefined(typeof(OpsAttackVector), technique.vector), technique.name);
                    Assert.IsNotNull(technique.summary, "要相談はnullでなく空文字にする");
                }
            }
        }

        [Test] public void 技の糸口の割当がNext20の表から変わらない()
        {
            var expected = new[] { "D,D,S,V", "P,M,C", "C,S,S,S", "V,V,V", "S,M,V", "S,D,S", "C,P,C", "V,P,M", "M,C,C,C", "S,S,P" };
            var symbols = new Dictionary<OpsAttackVector, string> {
                { OpsAttackVector.Vulnerability, "V" }, { OpsAttackVector.Malware, "M" }, { OpsAttackVector.PasswordTheft, "P" },
                { OpsAttackVector.Misconfiguration, "C" }, { OpsAttackVector.DataEncryption, "D" }, { OpsAttackVector.SocialEngineering, "S" }
            };
            CollectionAssert.AreEqual(expected, OpsThreatCatalog.Threats.Select(t => string.Join(",", t.techniques.Select(x => symbols[x.vector]))));
        }

        [Test] public void 効く設備の参照は現在の設備カタログにすべて存在する()
        {
            foreach (var threat in OpsThreatCatalog.Threats)
            {
                Assert.IsNotEmpty(threat.weaknesses);
                Assert.AreEqual(threat.weaknesses.Count, threat.weaknesses.Distinct().Count());
                foreach (string id in threat.weaknesses) Assert.GreaterOrEqual(OpsCatalog.Index(id), 0, threat.ipaName + ": " + id);
            }
        }

        [Test] public void 六つの糸口すべてに実在する備えがあり脅威でも使われる()
        {
            CollectionAssert.AreEqual(new[] { "patch", "edr", "mfa", "inventory", "backup", "education" }, OpsThreatCatalog.Vectors.Select(v => v.equipmentIds.Single()));
            foreach (var vector in OpsThreatCatalog.Vectors)
            {
                Assert.IsNotEmpty(vector.equipmentIds);
                foreach (string id in vector.equipmentIds)
                {
                    Assert.GreaterOrEqual(OpsCatalog.Index(id), 0, id);
                    Assert.IsTrue(OpsThreatCatalog.Threats.Any(t => t.techniques.Any(x => x.vector == vector.vector) && t.weaknesses.Contains(id)), vector.name + ": " + id);
                }
            }
        }

        [Test] public void 今の七強敵とのつながりだけを保持する()
        {
            var existing = OpsCatalog.StoryCompanies.Where(y => y.rivals != null).SelectMany(y => y.rivals).Select(r => r.id).ToArray();
            Assert.AreEqual(7, existing.Length);
            CollectionAssert.AreEquivalent(existing, OpsThreatCatalog.Threats.SelectMany(t => t.rivalIds));
            CollectionAssert.AreEqual(new[] { "y2-ransom,y3-final", "y2-supply", "y2-ai,y3-ai", "", "y3-targeted", "", "", "", "", "y3-bec" }, OpsThreatCatalog.Threats.Select(t => string.Join(",", t.rivalIds)));
        }

        [Test] public void 十色は重複せず指定の色と仮名と瞳を保持する()
        {
            Assert.AreEqual(10, OpsThreatCatalog.Threats.Select(t => t.color).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            CollectionAssert.AreEqual(new[] { "#D7263D", "#14A098", "#8E44AD", "#8BC34A", "#29B6F6", "#F4511E", "#C2185B", "#455A64", "#1E63D6", "#F5A623" }, OpsThreatCatalog.Threats.Select(t => t.color));
            CollectionAssert.AreEqual(new[] { "クリプタ", "サプラ", "ハルシ", "ヴァルネラ", "スピア", "コグニ", "プリヴィ", "ヴィピ", "フラッダ", "スプーフィ" }, OpsThreatCatalog.Threats.Select(t => t.charName));
            foreach (var threat in OpsThreatCatalog.Threats)
            {
                Assert.IsTrue(Regex.IsMatch(threat.color, "^#[0-9A-F]{6}$"));
                Assert.IsFalse(string.IsNullOrWhiteSpace(threat.nickname));
                Assert.IsFalse(string.IsNullOrWhiteSpace(threat.pupil));
            }
        }

        [Test] public void 全件にIPA公式の出典と確認日がある()
        {
            foreach (var threat in OpsThreatCatalog.Threats)
            {
                Assert.AreEqual("https://www.ipa.go.jp/security/10threats/10threats2026.html", threat.source);
                Assert.AreEqual("2026-10-03", threat.@checked);
            }
            Assert.AreEqual("https://www.ipa.go.jp/security/10threats/omgdg50000008fi8-att/kaisetsu_2026_soshiki.pdf", OpsThreatCatalog.ExplanationSource);
        }

        [Test] public void 資料の配列と要素は書き換えられずUnityの型を持たない()
        {
            Assert.Throws<NotSupportedException>(() => ((IList<OpsThreat>)OpsThreatCatalog.Threats)[0] = null);
            Assert.Throws<NotSupportedException>(() => ((IList<OpsAttackVectorDefinition>)OpsThreatCatalog.Vectors)[0] = null);
            foreach (var threat in OpsThreatCatalog.Threats)
            {
                Assert.Throws<NotSupportedException>(() => ((IList<OpsTechnique>)threat.techniques)[0] = null);
                Assert.Throws<NotSupportedException>(() => ((IList<string>)threat.weaknesses)[0] = "missing");
                Assert.Throws<NotSupportedException>(() => ((IList<string>)threat.rivalIds).Add("missing"));
            }
            foreach (var vector in OpsThreatCatalog.Vectors)
                Assert.Throws<NotSupportedException>(() => ((IList<string>)vector.equipmentIds)[0] = "missing");
            foreach (var type in new[] { typeof(OpsThreat), typeof(OpsTechnique), typeof(OpsAttackVectorDefinition), typeof(OpsThreatCatalog) })
                foreach (var field in type.GetFields())
                {
                    Assert.IsTrue(field.IsInitOnly || field.IsLiteral, type.Name + "." + field.Name);
                    Assert.IsFalse((field.FieldType.FullName ?? "").Contains("UnityEngine"));
                }
        }
    }
}
#endif
