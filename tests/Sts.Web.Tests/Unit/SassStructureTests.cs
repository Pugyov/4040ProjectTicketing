using Xunit;
using System.Text.RegularExpressions;

namespace Sts.Web.Tests.Unit;

public class SassStructureTests
{
    [Fact]
    public void SiteSass_ShouldComposeConcernBasedModules()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var siteSassPath = Path.Combine(projectRoot, "src", "Sts.Web", "wwwroot", "sass", "site.sass");

        Assert.True(File.Exists(siteSassPath));

        var source = File.ReadAllText(siteSassPath);

        Assert.Contains("@use \"utils/utils\"", source);
        Assert.Contains("@use \"base/base\"", source);
        Assert.Contains("@use \"components/components\"", source);
        Assert.Contains("@use \"pages/pages\"", source);
    }

    [Fact]
    public void SiteNavStyles_ShouldHandleBootstrapCollapseTransitionStates()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var siteNavPath = Path.Combine(projectRoot, "src", "Sts.Web", "wwwroot", "sass", "components", "_site-nav.sass");

        Assert.True(File.Exists(siteNavPath));

        var source = File.ReadAllText(siteNavPath);

        Assert.Contains(".sts-site-nav--menu-open", source);
        Assert.Contains(".sts-site-nav__panel", source);
        Assert.Contains(".sts-site-nav--menu-open .sts-site-nav__collapse", source);
        Assert.Contains(".sts-site-nav--menu-open .sts-site-nav__panel", source);
        Assert.Contains("grid-template-rows", source);
        Assert.Contains("opacity:", source);
        Assert.Contains("transform:", source);
        Assert.DoesNotContain("&__", source);
        Assert.DoesNotContain("$nav-", source);
    }

    [Fact]
    public void SiteNavStyles_ShouldKeepDesktopActionsRightAligned()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var siteNavPath = Path.Combine(projectRoot, "src", "Sts.Web", "wwwroot", "sass", "components", "_site-nav.sass");

        Assert.True(File.Exists(siteNavPath));

        var source = File.ReadAllText(siteNavPath);

        Assert.Contains("@include mq.media-lg", source);
        Assert.Contains("justify-content: flex-end", source);
        Assert.Contains("width: 100%", source);
    }

    [Fact]
    public void SiteNavStyles_ShouldUseShallowIndentation()
    {
        var sassRoot = GetSassRoot();
        var siteNavPath = Path.Combine(sassRoot, "components", "_site-nav.sass");

        Assert.True(File.Exists(siteNavPath));

        foreach (var line in File.ReadAllLines(siteNavPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var leadingSpaces = line.TakeWhile(ch => ch == ' ').Count();
            Assert.True(leadingSpaces <= 4, $"Line exceeds shallow indentation rule: '{line}'");
        }
    }

    [Fact]
    public void SiteNavScript_ShouldUseCustomMenuStateInsteadOfBootstrapCollapseEvents()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var scriptPath = Path.Combine(projectRoot, "src", "Sts.Web", "wwwroot", "js", "site-nav.js");

        Assert.True(File.Exists(scriptPath));

        var source = File.ReadAllText(scriptPath);

        Assert.Contains("sts-site-nav--menu-open", source);
        Assert.DoesNotContain("show.bs.collapse", source);
        Assert.DoesNotContain("hide.bs.collapse", source);
    }

    [Fact]
    public void SassSpacing_ShouldUsePixelsForPaddingAndMarginDeclarations()
    {
        var sassRoot = GetSassRoot();
        var sources = Directory.GetFiles(sassRoot, "*.sass", SearchOption.AllDirectories)
            .Select(path => File.ReadAllText(path));

        var remSpacingPattern = new Regex(@"(?:padding|margin)(?:-[a-z]+)?:\s*[^\n]*rem", RegexOptions.Compiled);

        foreach (var source in sources)
        {
            Assert.DoesNotMatch(remSpacingPattern, source);
        }
    }

    [Fact]
    public void SassTypography_ShouldUseRemFontSizesAndOneSharedLineHeight()
    {
        var sassRoot = GetSassRoot();
        var files = Directory.GetFiles(sassRoot, "*.sass", SearchOption.AllDirectories);
        var fontSizePattern = new Regex(@"font-size:\s*([^\n]+)", RegexOptions.Compiled);

        var lineHeightCount = 0;

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);

            foreach (Match match in fontSizePattern.Matches(source))
            {
                var value = match.Groups[1].Value.Trim();
                Assert.DoesNotContain("clamp(", value);
                Assert.Matches(@"^(?:\d*\.?\d+rem|(?:variables|vars)\.\$font-size-[\w-]+)$", value);
            }

            lineHeightCount += Regex.Matches(source, @"line-height:").Count;
        }

        Assert.Equal(1, lineHeightCount);

        var globalsPath = Path.Combine(sassRoot, "base", "_globals.sass");
        var globalsSource = File.ReadAllText(globalsPath);
        Assert.Contains("line-height:", globalsSource);
    }

    [Fact]
    public void HomePageStyles_ShouldAvoidPerPropertySpacingVariables()
    {
        var sassRoot = GetSassRoot();
        var homePath = Path.Combine(sassRoot, "pages", "_home.sass");

        Assert.True(File.Exists(homePath));

        var source = File.ReadAllText(homePath);

        Assert.DoesNotContain("$home-", source);
    }

    [Fact]
    public void GlobalVariables_ShouldContainSharedSpacingAndTypeTokens()
    {
        var sassRoot = GetSassRoot();
        var variablesPath = Path.Combine(sassRoot, "utils", "_variables.sass");

        Assert.True(File.Exists(variablesPath));

        var source = File.ReadAllText(variablesPath);

        Assert.Contains("$space-5:", source);
        Assert.Contains("$space-10:", source);
        Assert.Contains("$space-12:", source);
        Assert.Contains("$space-15:", source);
        Assert.Contains("$space-20:", source);
        Assert.Contains("$font-size-base:", source);
        Assert.Contains("$font-size-copy-lg:", source);
        Assert.Contains("$font-size-nav-brand:", source);
        Assert.Contains("$font-size-section-heading:", source);
        Assert.Contains("$line-height-base:", source);
    }

    private static string GetSassRoot()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(projectRoot, "src", "Sts.Web", "wwwroot", "sass");
    }
}
