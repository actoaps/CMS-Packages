<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()" />
		</xsl:copy>
	</xsl:template>
	<xsl:template match="/configuration/Composite.Functions.Plugins.XslExtensionsProviderConfiguration/XslExtensionProviders/add[@name='ConfigurationBasedXslExtensionsProvider']/xslExtensions/add[@name='http://c1.composite.net/News']" />
</xsl:stylesheet>