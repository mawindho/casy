<?xml version="1.0" ?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

  <!-- Copy all attributes and elements to the output. -->
  <xsl:template match="@*|*">
    <xsl:copy>
      <xsl:apply-templates select="@*" />
      <xsl:apply-templates select="*" />
    </xsl:copy>
  </xsl:template>

  <xsl:output method="xml" indent="yes" />

  <xsl:key name="txt_search" match="wix:Component[(contains(wix:File/@Source, '.txt'))]" use="@Id"/>
  <xsl:template match="wix:Component[(contains(wix:File/@Source, '.txt'))]"/>
  <xsl:template match="wix:ComponentRef[key('txt_search', @Id)]" />

  <xsl:key name="db_search" match="wix:Component[(contains(wix:File/@Source, '.db'))]" use="@Id"/>
  <xsl:template match="wix:Component[(contains(wix:File/@Source, '.db'))]"/>
  <xsl:template match="wix:ComponentRef[key('db_search', @Id)]" />

  <xsl:key name="Data" match="wix:Directory[@Name = 'Data']" use="@Id" />
  <xsl:template match="wix:Directory[@Name='Data']" />
  <xsl:template match="wix:DirectoryRef[key('Data', @Directory)]" />
  <xsl:template match="wix:Component[key('Data', @Directory)]" />

  <xsl:key name="temp" match="wix:Directory[@Name = 'temp']" use="@Id" />
  <xsl:template match="wix:Directory[@Name='temp']" />
  <xsl:template match="wix:Component[key('temp', @Directory)]" />

  <xsl:key name="Backup" match="wix:Directory[@Name = 'Backup']" use="@Id" />
  <xsl:template match="wix:Directory[@Name='Backup']" />
  <xsl:template match="wix:Component[key('Backup', @Directory)]" />

  <xsl:key name="Calibration" match="wix:Directory[@Name = 'Calibration']" use="@Id" />
  <xsl:template match="wix:Directory[@Name='Calibration']" />
  <xsl:template match="wix:Component[key('Calibration', @Directory)]" />

  <!-- Remove directories. -->
  <!--<xsl:template match="wix:Directory[@Name='temp']" />
  <xsl:template match="wix:Directory[@Name='Data']" />
  <xsl:template match="wix:Directory[@Name='netstandard2.0']" />-->
  
  <!-- Remove Components referencing those directories. -->
  <!--<xsl:template match="wix:Component[key('temp', @Directory)]" />
  <xsl:template match="wix:Component[key('Data', @Directory)]" />
  <xsl:template match="wix:Component[key('netstandard2.0', @Directory)]" />-->
  
  <!-- Remove DirectoryRefs (and their parent Fragments) referencing those directories. -->
  <!--<xsl:template match="wix:Fragment[wix:DirectoryRef[key('temp', @Id)]]" />
  <xsl:template match="wix:Fragment[wix:DirectoryRef[key('Data', @Id)]]" />
  <xsl:template match="wix:Fragment[wix:DirectoryRef[key('netstandard2.0', @Id)]]" />-->
</xsl:stylesheet>