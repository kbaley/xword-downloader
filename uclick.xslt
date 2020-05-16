<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" 
    xmlns:my="http://baley.org/my"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="html" version="4.0" encoding="UTF-8" indent="yes"/>
 <xsl:variable name="vPats">
  <pattern>
   <old>%27</old>
   <new>'</new>
  </pattern>
  <pattern>
   <old>%22</old>
   <new>"</new>
  </pattern>
  <pattern>
    <old>%2F</old>
    <new>/</new>
  </pattern>
 </xsl:variable>
    <xsl:template match="crossword">
        <html>

            <head>
                <style>
        @media print {
            @page { margin: 0; }
            body { margin: 1.6cm }
        }
        @media screen {
            body { max-width: 710px; }
        }
        body {
            font-family: Helvetica, Arial, sans-serif;
        }
        .page {
            display: grid;
            grid-template-columns: 67% 33%;
        }
        .puzzle-header {
            text-align: center;
            margin: 15px;
        }
        .puzzle-title {
            font-size: 18px;
            font-weight: 500;
        }
        .puzzle-provider {
            font-size: 27px;
            font-weight: 600;
        }
        .grid {
            grid-column: 1;
            grid-row: 2;
            display: grid;
            margin: 10px;
            grid-template-columns: repeat(<xsl:value-of select="Width/@v" />, 30px);
            grid-template-rows: repeat(<xsl:value-of select="Height/@v" />, 30px);
        }
        .box {
            border: 1px solid #999;
            height: 29px;
            width: 29px;
            padding: 0;
        }
        .filled {
            background-color: #999;
        }
        .box:after {
            content: attr(clue);
            display: inline-block;
            font-size: 10px;
            margin: 0 0 0 2px;
        }
        .clues {
            border: 2px solid black;
            margin: 5px;
            padding: 12px;
            font-size: 11px; 
        }
        .across {
            grid-column: 1;
            display: grid;
            grid-template-columns: 50% 50%;
            gap: 10px;
        }
        .down {
            grid-column: 2;
            grid-row: 1 / span 2;
            float: right;
        }
        .header {
            grid-column: 1 / span 2;
        }
        .clue-header {
            margin: 0;
            font-size: 16px;
        }
        .clue-number {
            font-weight: 600;
            margin: 0 5px 0 0;
        }
            </style>
        </head>

        <body>
            <div class="page">
                <div class="across clues">
                    <div class="header clue-header">ACROSS</div>
                    <xsl:variable name="mid" select="round(count(across/*) div 2)" />
                    <div class="content">
                        <xsl:for-each select="across/*[position() &lt;= $mid]">
                            <div class="clue">
                                <span class="clue-number"><xsl:value-of select="./@cn" />)</span> <xsl:call-template name="multiReplace">
                                            <xsl:with-param name="pText" select="./@c" />
                                        </xsl:call-template>
                            </div>
                        </xsl:for-each>
                    </div>
                    <div class="content">
                        <xsl:for-each select="across/*[position() &gt; $mid]">
                            <div class="clue">
                                <span class="clue-number"><xsl:value-of select="./@cn" />)</span> <xsl:call-template name="multiReplace">
                                            <xsl:with-param name="pText" select="./@c" />
                                        </xsl:call-template>
                            </div>
                        </xsl:for-each>
                    </div>
                </div>
                <div class="down">
                    <div class="puzzle-header">
                        <div class="puzzle-provider">
                            <xsl:value-of select="Category/@v" />
                        </div>
                        <div class="puzzle-title">
                            <xsl:value-of select="Title/@v" />
                        </div>
                        <div class="puzzle-author">
                            <xsl:value-of select="Author/@v" />
                        </div>
                        <div class="puzzle-editor">Edited By <xsl:value-of select="Editor/@v" />
                        </div>
                    </div>
                    <div class="clues">
                        <div class="clue-header">DOWN</div>
                        <div class="content">
                        <xsl:for-each select="down/*">
                            <div class="clue">
                                <span class="clue-number"><xsl:value-of select="./@cn" />)</span> <xsl:call-template name="multiReplace">
                                            <xsl:with-param name="pText" select="./@c" />
                                        </xsl:call-template>
                            </div>
                        </xsl:for-each>
                        </div>
                    </div>
                </div>
                <div class="grid">
                    <xsl:call-template name="grid">
                        <xsl:with-param name="text" select="AllAnswer/@v" />
                        <xsl:with-param name="position" select="1" />
                    </xsl:call-template>
                </div>
            </div>
        </body>

    </html>
</xsl:template>

<xsl:template name="multiReplace">
  <xsl:param name="pText" select="."/>
  <xsl:param name="pPatterns" select="msxsl:node-set($vPats)/pattern"/>

  <xsl:if test="string-length($pText)>0">

    <xsl:variable name="vPat" select=
     "$pPatterns[starts-with($pText, old)][1]"/>
    <xsl:choose>
     <xsl:when test="not($vPat)">
       <xsl:copy-of select="substring($pText,1,1)"/>
     </xsl:when>
     <xsl:otherwise>
       <xsl:copy-of select="$vPat/new/node()"/>
     </xsl:otherwise>
    </xsl:choose>

    <xsl:call-template name="multiReplace">
      <xsl:with-param name="pText" select=
       "substring($pText, 1 + not($vPat) + string-length($vPat/old/node()))"/>
    </xsl:call-template>
  </xsl:if>
 </xsl:template>

<xsl:template name="decode">
    <xsl:param name="s" />
    <xsl:variable name="apos">'</xsl:variable>
    <xsl:variable name="quot">"</xsl:variable>
    <xsl:variable name="return">
        <xsl:call-template name="string-replace-all">
            <xsl:with-param name="text" select="$s" />
            <xsl:with-param name="replace" select="'%27'" />
            <xsl:with-param name="by" select="$apos" />
        </xsl:call-template>
    </xsl:variable>
    <xsl:value-of select="$return" />
</xsl:template>

<xsl:template name="string-replace-all">
    <xsl:param name="text" />
    <xsl:param name="replace" />
    <xsl:param name="by" />
    <xsl:choose>
        <xsl:when test="$text = '' or $replace = ''or not($replace)" >
            <!-- Prevent this routine from hanging -->
            <xsl:value-of select="$text" />
        </xsl:when>
        <xsl:when test="contains($text, $replace)">
            <xsl:value-of select="substring-before($text,$replace)" />
            <xsl:value-of select="$by" />
            <xsl:call-template name="string-replace-all">
                <xsl:with-param name="text" select="substring-after($text,$replace)" />
                <xsl:with-param name="replace" select="$replace" />
                <xsl:with-param name="by" select="$by" />
            </xsl:call-template>
        </xsl:when>
        <xsl:otherwise>
            <xsl:value-of select="$text" />
        </xsl:otherwise>
    </xsl:choose>
</xsl:template>

<xsl:template name="grid">
    <xsl:param name="text" />
    <xsl:param name="position" />
    <xsl:if test="$text != ''">
        <xsl:variable name="letter" select="substring($text, 1, 1)" />
        <xsl:variable name="clue" select="*/*[@n=$position]/@cn" />

        <xsl:choose>
            <xsl:when test="$letter = '-'">
                <div class="filled box"></div>
            </xsl:when>
            <xsl:otherwise>

                <div class="box">
                    <xsl:attribute name="clue">
                        <xsl:value-of select="$clue" />
                    </xsl:attribute>
                </div>
            </xsl:otherwise>
        </xsl:choose>
        <xsl:call-template name="grid">
            <xsl:with-param name="text" select="substring-after($text, $letter)" />
            <xsl:with-param name="position" select="$position+1" />
        </xsl:call-template>
    </xsl:if>
</xsl:template>
</xsl:stylesheet>

